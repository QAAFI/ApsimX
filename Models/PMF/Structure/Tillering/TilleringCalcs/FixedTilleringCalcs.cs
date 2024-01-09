using APSIM.Shared.Utilities;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.PMF.Struct;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.PMF
{
    /// <summary>Fixed Tillering Calculations</summary>
    [Serializable]
    public class FixedTilleringCalcs : ITilleringCalcs
    {
        /// <summary>The parent Plant</summary>
        protected readonly Plant plant = null;

        /// <summary> Culms on the leaf </summary>
        public LeafCulms culms = null;

        /// <summary>The parent tilering class</summary>
        protected readonly Phenology phenology = null;

        /// <summary>The parent tilering class</summary>
        protected readonly SorghumLeaf leaf = null;

        /// <summary>The met data</summary>
        private readonly IWeather weather = null;

        /// <summary> Culms on the leaf </summary>
        protected readonly IFunction areaCalc = null;

        /// <summary> Propoensity to Tiller Intercept </summary>
        private readonly IFunction tillerSdIntercept = null;

        /// <summary> Propsenity to Tiller Slope </summary>
        private readonly IFunction tillerSdSlope = null;

        /// <summary> LAI Value where tillers are no longer added </summary>
        protected readonly IFunction maxLAIForTillerAddition = null;

        /// <summary>If we are performing Fixed tillering then this will be the number of Fertile Tillers that was specified in the simulation.</summary>
        private readonly double fixedTilleringFTN;

        /// <summary>Number of potential Fertile Tillers at harvest</summary>
        public double CalculatedTillerNumber { get; set; }

        /// <summary>Current Number of Tillers</summary>
        public double DltTillerNumber { get; set; }

        /// <summary>Actual Number of Fertile Tillers</summary>
        public double FertileTillerNumber { get; set; }

        /// <summary>The Linear LAI.</summary>
        public double LinearLAI { get; protected set; }

        /// <summary>The number of plants in one square metre.</summary>
        protected double PlantsPerMetre { get; set; }

        /// <summary>The plant population.</summary>
        protected double Population { get; set; }

        /// <summary>Keeps track of the radiation values.</summary>
        protected double RadiationValues { get; set; }

        /// <summary>Keeps track of the temprature values.</summary>
        protected double TemperatureValues { get; set; }

        /// <summary>The order that tillers will be added.</summary>
        protected List<int> TillerOrder { get; set; } = new ();

        /// <summary>The start of the period during which average PTQ is calculated</summary>
        protected const int START_THERMAL_QUOTIENT_LEAF_NO = 3;

        /// <summary>The end of the period during which average PTQ is calculated</summary>
        protected const int END_THERMAL_QUOTIENT_LEAF_NO = 5;

        /// <summary>Constuctor</summary>
        public FixedTilleringCalcs(
            Plant plant,
            LeafCulms culms,
            Phenology phenology,
            SorghumLeaf leaf,
            IWeather weather,
            IFunction areaCalc,
            IFunction tillerSdIntercept,
            IFunction tillerSdSlope,
            IFunction maxLAIForTillerAddition,
            double fixedTilleringFTN
        )
        {
            this.plant = plant;
            this.culms = culms;
            this.phenology = phenology;
            this.leaf = leaf;
            this.weather = weather;
            this.areaCalc = areaCalc;
            this.tillerSdIntercept = tillerSdIntercept;
            this.tillerSdSlope = tillerSdSlope;
            this.maxLAIForTillerAddition = maxLAIForTillerAddition;
            this.fixedTilleringFTN = fixedTilleringFTN;
        }

        #region Public Interface

        /// <summary>Called when a StartOfSim event is captured from the tillering classes</summary>
        public void StartOfSim()
        {
            FertileTillerNumber = 0.0;
            CalculatedTillerNumber = 0.0;
            DltTillerNumber = 0.0;
        }

        /// <summary>Called when an OnSowing event is captured from the tillering classes</summary>
        public void HandleOnPlantSowing(SowingParameters sowingParameters)
        {
            Population = sowingParameters.Population;
            PlantsPerMetre = sowingParameters.Population * sowingParameters.RowSpacing / 1000.0 * sowingParameters.SkipDensityScale;
            FertileTillerNumber = 0.0;
            CalculatedTillerNumber = 0.0;
            RadiationValues = 0.0;
            TemperatureValues = 0.0;
        }

        /// <summary>Calculate number of leaves</summary>
        public virtual double CalcLeafNumber()
        {
            if (culms.Culms?.Count == 0) return 0.0;
            if (!plant.IsEmerged) return 0.0;

            if (phenology.BeforeEndJuvenileStage())
            {
                // ThermalTime Targets to EndJuv are not known until the end of the Juvenile Phase
                // FinalLeafNo is not known until the TT Target is known - meaning the potential leaf sizes aren't known
                culms.Culms.ForEach(c => c.UpdatePotentialLeafSizes(areaCalc as ICulmLeafArea));
            }

            var mainCulm = culms.Culms[0];
            var existingLeafNo = (int)Math.Floor(mainCulm.CurrentLeafNo);
            var nLeaves = mainCulm.CurrentLeafNo;
            var dltLeafNoMainCulm = 0.0;
            culms.dltLeafNo = dltLeafNoMainCulm;

            if (phenology.BeforeStartOfGrainFillStage())
            {
                // Calculate the leaf apperance on the main culm.
                dltLeafNoMainCulm = CalcLeafAppearance(mainCulm);

                // Now calculate the leaf apperance on all of the other culms.
                for (int i = 1; i < culms.Culms.Count; i++)
                {
                    CalcLeafAppearance(culms.Culms[i]);
                }
            }

            var newLeafNo = (int)Math.Floor(mainCulm.CurrentLeafNo);

            if (nLeaves > START_THERMAL_QUOTIENT_LEAF_NO)
            {
                CalcTillers(newLeafNo, existingLeafNo);
                CalcTillerAppearance(newLeafNo, existingLeafNo);
            }

            return dltLeafNoMainCulm;
        }

        /// <summary>Calculates the potential leaf area.</summary>
        public double CalcPotentialLeafArea()
        {
            culms.Culms.ForEach(c => c.DltLAI = 0);
            if (phenology.BeforeFloweringStage())
            {
                return areaCalc.Value();
            }
            return 0.0;
        }

        /// <summary>Calculate the actual leaf area</summary>
        public virtual double CalcActualLeafArea(double dltStressedLAI)
        {
            var mainCulm = culms.Culms.FirstOrDefault();
            double laiSlaReductionFraction = CalcCarbonLimitation(dltStressedLAI);
            var dltLAI = Math.Max(dltStressedLAI * laiSlaReductionFraction, 0.0);
            culms.Culms.ForEach(c => c.TotalLAI += c.DltStressedLAI);
            return dltLAI;
        }

        #endregion

        #region Protected Interface

        /// <summary>Calculates the carbon limitation.</summary>
        protected double CalcCarbonLimitation(double dltStressedLAI)
        {
            double dltDmGreen = leaf.potentialDMAllocation.Structural;
            if (dltDmGreen <= 0.001) return 1.0;

            var mainCulm = culms.Culms[0];

            // Changing to Reeves + 10%
            double nLeaves = mainCulm.CurrentLeafNo;
            double maxSLA;
            maxSLA = 429.72 - 18.158 * (nLeaves);
            maxSLA = Math.Min(400, maxSLA);
            maxSLA = Math.Max(150, maxSLA);
            var dltLaiPossible = dltDmGreen * maxSLA / 10000.0;

            double fraction = Math.Min(dltStressedLAI > 0 ? (dltLaiPossible / dltStressedLAI) : 1.0, 1.0);
            return fraction;
        }

        #endregion

        #region Private Interface

        /// <summary>Calculates the leaf appearance.</summary>
        private double CalcLeafAppearance(Culm culm)
        {
            var leavesRemaining = culms.FinalLeafNo - culm.CurrentLeafNo;
            var leafAppearanceRate = culms.getLeafAppearanceRate(leavesRemaining);
            // If leaves are still growing, the cumulative number of phyllochrons or fully expanded leaves is calculated from thermal time for the day.
            var dltLeafNo = MathUtilities.Bound(MathUtilities.Divide(phenology.thermalTime.Value(), leafAppearanceRate, 0), 0.0, leavesRemaining);

            culm.AddNewLeaf(dltLeafNo);

            return dltLeafNo;
        }

        private void CalcTillers(int newLeaf, int currentLeaf)
        {
            if (CalculatedTillerNumber > 0.0) return;

            // Up to L5 FE store PTQ. At L5 FE calculate tiller number (endThermalQuotientLeafNo).
            // At L5 FE newLeaf = 6 and currentLeaf = 5
            if (newLeaf >= START_THERMAL_QUOTIENT_LEAF_NO &&
                currentLeaf < END_THERMAL_QUOTIENT_LEAF_NO)
            {
                RadiationValues += weather.Radn;
                TemperatureValues += phenology.thermalTime.Value();

                // L5 Fully Expanded
                if (newLeaf == END_THERMAL_QUOTIENT_LEAF_NO)
                {
                    double PTQ = RadiationValues / TemperatureValues;
                    CalcTillerNumber(PTQ);
                    AddInitialTillers();
                }
            }
        }

        /// <summary>Calculates the tiller number.</summary>
        private void CalcTillerNumber(double PTQ)
        {
            // The final tiller number (Ftn) is calculated after the full appearance of LeafNo 5 - when leaf 6 emerges.
            // Calc Supply = R/oCd * LA5 * Phy5
            var areaMethod = areaCalc as ICulmLeafArea;
            var mainCulm = culms.Culms[0];
            double L5Area = areaMethod.CalculateIndividualLeafArea(5, mainCulm);
            double L9Area = areaMethod.CalculateIndividualLeafArea(9, mainCulm);

            double Phy5 = culms.getLeafAppearanceRate(culms.FinalLeafNo - culms.Culms[0].CurrentLeafNo);

            // Calc Demand = LA9 - LA5
            var demand = L9Area - L5Area;
            var supply = PTQ * L5Area * Phy5;
            var supplyDemandRatio = MathUtilities.Divide(supply, demand, 0);
            // Calculate the tiller number using the intercept and slope values.
            var calculatedValue = tillerSdIntercept.Value() + tillerSdSlope.Value() * supplyDemandRatio;

            // If we've got a fixed tillering FTN, then we need to limit it based on this.
            if (fixedTilleringFTN > 0.0)
            {
                calculatedValue = Math.Min(fixedTilleringFTN, calculatedValue);
            }

            CalculatedTillerNumber = Math.Max(calculatedValue, 0.0);
        }

        private void CalcTillerAppearance(int newLeaf, int currentLeaf)
        {
            // Each time a leaf becomes fully expanded starting at 5 see if a tiller should be initiated.
            // When a leaf is fully expanded a tiller can be initiated at the axil 3 leaves less
            // So at L5 FE (newLeaf = 6, currentLeaf = 5) a Tiller might be at axil 2. i.e. a T2 

            // Add any new tillers and then calc each tiller in turn. Add a tiller if:
            // 1. There are more tillers to add.
            // 2. linearLAI < maxLAIForTillerAddition
            // 3. A leaf has fully expanded.  (newLeaf >= 6, newLeaf > currentLeaf)
            // 4. there should be a tiller at that node. (Check tillerOrder)

            var tillersAdded = culms.Culms.Count - 1;
            LinearLAI = CalcLinearLAI();

            if (newLeaf >= 5 &&
                newLeaf > currentLeaf &&
                CalculatedTillerNumber > tillersAdded &&
                LinearLAI < maxLAIForTillerAddition.Value()
            )
            {
                // Axil = currentLeaf - 3
                int newNodeNumber = newLeaf - 3;
                if (TillerOrder.Contains(newNodeNumber))
                {
                    //var tillerUpperLimit = CalculatedTillerNumber;

                    //if (fixedTilleringFTN > 0)
                    //{
                    //    tillerUpperLimit = Math.Min(CalculatedTillerNumber, fixedTilleringFTN);
                    //}

                    //var fractionToAdd = Math.Min(1.0, tillerUpperLimit - tillersAdded);
                    var fractionToAdd = Math.Min(1.0, CalculatedTillerNumber - tillersAdded);

                    DltTillerNumber = fractionToAdd;
                    FertileTillerNumber += fractionToAdd;
                    InitiateTiller(newNodeNumber, fractionToAdd, 1);
                }
            }
        }

        /// <summary>Calculates the Linear LAI</summary>
        private double CalcLinearLAI()
        {
            // Leaf area of one plant.
            var tpla = (leaf.LAI + leaf.SenescedLai) / Population * 10000;
            var linearLAI = PlantsPerMetre * tpla / 10000.0;
            return linearLAI;
        }

        /// <summary>Adds the initial tillers.</summary>
        private void AddInitialTillers()
        {
            CalculateTillerOrder();

            if (CalculatedTillerNumber <= 0) return;

            int nTillers = (int)Math.Ceiling(CalculatedTillerNumber);

            if (nTillers > 3)
            {
                InitiateTiller(1, 1, 2);
                FertileTillerNumber = 1;
            }
        }

        /// <summary>
        /// Sets the order that tillers appear, according to the total tillers
        /// Lafarge et al. (2002) reported a common hierarchy of tiller emergence of T3>T4>T2>T1>T5>T6 across diverse density treatments
        /// 1 tiller  = T3 
        /// 2 tillers = T3 + T4
        /// 3 tillers = T2 + T3 + T4
        /// 4 tillers = T1 + T2 + T3 + T4
        /// 5 tillers = T1 + T2 + T3 + T4 + T5
        /// 6 tillers = T1 + T2 + T3 + T4 + T5 + T6
        /// </summary>
        private void CalculateTillerOrder()
        {
            TillerOrder.Clear();

            if (CalculatedTillerNumber <= 0) return;

            // At leaf 5 fully expanded only initialize T1 with 2 leaves if present.

            int nTillers = (int)Math.Ceiling(CalculatedTillerNumber);
            if (nTillers <= 0) return;

            if (nTillers < 3) TillerOrder.Add(3);
            if (nTillers == 2) TillerOrder.Add(4);
            if (nTillers == 3)
            {
                TillerOrder.Add(2);
                TillerOrder.Add(3);
                TillerOrder.Add(4);
            }
            if (nTillers > 3)
            {
                for (int i = 1; i <= nTillers; i++)
                {
                    TillerOrder.Add(i);
                }
            }
        }

        /// <summary>Calculates the potential leaf area.</summary>
        private void InitiateTiller(
            int tillerNumber,
            double fractionToAdd,
            double initialLeaf
        )
        {
            double leafNoAtAppearance = 1.0;
            Culm newCulm = new(leafNoAtAppearance)
            {
                CulmNo = tillerNumber,
                CurrentLeafNo = initialLeaf,
                VertAdjValue = culms.MaxVerticalTillerAdjustment.Value() + (FertileTillerNumber * culms.VerticalTillerAdjustment.Value()),
                Proportion = fractionToAdd,
                FinalLeafNo = culms.Culms[0].FinalLeafNo
            };
            newCulm.UpdatePotentialLeafSizes(areaCalc as ICulmLeafArea);
            culms.Culms.Add(newCulm);
        }

        #endregion
    }
}
