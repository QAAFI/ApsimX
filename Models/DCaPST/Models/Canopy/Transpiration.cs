using Models.DCAPST.Interfaces;

namespace Models.DCAPST.Canopy
{
    /// <summary>
    /// 
    /// </summary>
    public class Transpiration
    {
        /// <summary>
        /// The canopy parameters
        /// </summary>
        private readonly ICanopyParameters canopy;

        /// <summary>
        /// The pathway parameters
        /// </summary>

        private readonly IPathwayParameters pathway;

        /// <summary>
        /// Models the leaf water interaction
        /// </summary>
        private readonly IWaterInteraction water;

        /// <summary>
        /// Models how the leaf responds to different temperatures
        /// </summary>
        private readonly TemperatureResponse leaf;

        /// <summary>
        /// Provides access to the leaf GmT value.
        /// </summary>
        public double LeafGmT => leaf.GmT;

        /// <summary>
        /// If the transpiration rate is limited
        /// </summary>
        public bool Limited { get; set; }

        /// <summary>
        /// The boundary heat conductance
        /// </summary>
        public double BoundaryHeatConductance { get; set; }

        /// <summary>
        /// Maximum transpiration rate
        /// </summary>
        public double MaxRate { get; set; }

        /// <summary>
        /// Fraction of water allocated
        /// </summary>
        public double Fraction { get; set; }

        /// <summary>
        /// Resistance to water
        /// </summary>
        public double Resistance { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="pathway"></param>
        /// <param name="water"></param>
        /// <param name="leaf"></param>
        public Transpiration(
            ICanopyParameters canopy,
            IPathwayParameters pathway,
            IWaterInteraction water,
            TemperatureResponse leaf
        )
        {
            this.canopy = canopy;
            this.pathway = pathway;
            this.water = water;
            this.leaf = leaf;
        }

        /// <summary>
        /// Sets the current conditions for transpiration
        /// </summary>
        public void SetConditions(ParameterRates At25C, double photons, double radiation)
        {
            leaf.SetConditions(At25C, photons);
            water.SetConditions(BoundaryHeatConductance, radiation);
        }

        /// <summary>
        /// Sets the temperature which is needed by the leaf and water interaction.
        /// </summary>
        /// <param name="leafTemperature"></param>
        public void SetLeafTemperature(double leafTemperature)
        {
            leaf.LeafTemperature = leafTemperature;
            water.LeafTemp = leafTemperature;
        }

        /// <summary>
        /// Signals that the temperature has been updated so that we can recalculate parameters.
        /// </summary>
        public void TemperatureUpdated()
        {
            water.RecalculateParams();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assimilation"></param>
        /// <param name="pathway"></param>
        /// <returns></returns>
        public AssimilationFunction UpdateA(IAssimilation assimilation, AssimilationPathway pathway)
        {
            var func = assimilation.GetFunction(pathway, leaf);

            if (Limited)
            {
                var molarMassWater = 18;
                var g_to_kg = 1000;
                var hrs_to_seconds = 3600;

                pathway.WaterUse = MaxRate * Fraction;
                var WaterUseMolsSecond = pathway.WaterUse / molarMassWater * g_to_kg / hrs_to_seconds;

                Resistance = water.LimitedWaterResistance(pathway.WaterUse);
                var Gt = water.TotalCO2Conductance(Resistance);

                func.Ci = canopy.AirCO2 - WaterUseMolsSecond * canopy.AirCO2 / (Gt + WaterUseMolsSecond / 2.0);
                func.Rm = 1 / (Gt + WaterUseMolsSecond / 2) + 1.0 / leaf.GmT;

                pathway.CO2Rate = func.Value();

                assimilation.UpdateIntercellularCO2(pathway, Gt, WaterUseMolsSecond);
            }
            else
            {
                pathway.IntercellularCO2 = this.pathway.IntercellularToAirCO2Ratio * canopy.AirCO2;

                func.Ci = pathway.IntercellularCO2;
                func.Rm = 1 / leaf.GmT;

                pathway.CO2Rate = func.Value();

                Resistance = water.UnlimitedWaterResistance(pathway.CO2Rate, canopy.AirCO2, pathway.IntercellularCO2);
                pathway.WaterUse = water.HourlyWaterUse(Resistance);
            }
            pathway.VPD = water.VPD;

            return func;
        }

        /// <summary>
        /// Updates the temperature of a pathway
        /// </summary>
        public void UpdateTemperature(AssimilationPathway pathway)
        {
            var leafTemp = water.LeafTemperature(Resistance);
            pathway.Temperature = (leafTemp + pathway.Temperature) / 2.0;
        }
    }
}
