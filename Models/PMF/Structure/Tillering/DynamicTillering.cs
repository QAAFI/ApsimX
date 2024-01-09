using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Newtonsoft.Json;
using System;

namespace Models.PMF.Struct
{
    /// <summary>
    /// This is a tillering method to control the number of tillers and leaf area
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LeafCulms))]
    public class DynamicTillering : Model, ITilleringMethod
    {
        /// <summary>The parent Plant</summary>
        [Link]
        private readonly Plant plant = null;

        /// <summary> Culms on the leaf </summary>
        [Link]
        public LeafCulms culms = null;

        /// <summary>The parent tilering class</summary>
        [Link]
        private readonly Phenology phenology = null;

        /// <summary>The parent tilering class</summary>
        [Link]
        private readonly SorghumLeaf leaf = null;

        /// <summary>The met data</summary>
        [Link]
        private readonly IWeather weather = null;

        /// <summary> Culms on the leaf </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction areaCalc = null;

        /// <summary> Propoensity to Tiller Intercept </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction tillerSdIntercept = null;

        /// <summary> Propsenity to Tiller Slope </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction tillerSdSlope = null;

        /// <summary> LAI Value where tillers are no longer added </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction maxLAIForTillerAddition = null;

        /// <summary> LAI Value where tillers are no longer added </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction maxDailyTillerReduction = null;

        /// <summary> LAI Value where tillers are no longer added </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction tillerSlaBound = null;

        /// <summary>Actual Number of Fertile Tillers</summary>
        [JsonIgnore]
        public double FertileTillerNumber
        {
            get => tilleringCalculator?.FertileTillerNumber ?? 0.0;
            set { throw new Exception("Cannot set the FertileTillerNumber for Dynamic Tillering. Make sure you set TilleringMethod before FertileTillerNmber"); }
        }

        /// <summary>Number of potential Fertile Tillers at harvest</summary>
        [JsonIgnore]
        public double CalculatedTillerNumber
        {
            get => tilleringCalculator?.CalculatedTillerNumber ?? 0.0;
        }

        [JsonIgnore]
        private ITilleringCalcs tilleringCalculator;

        /// <summary> Calculate number of leaves</summary>
        public double CalcLeafNumber()
        {
            return tilleringCalculator.CalcLeafNumber();
        }

        /// <summary>Calculate the potential leaf area</summary>
        public double CalcPotentialLeafArea()
        {
            return tilleringCalculator.CalcPotentialLeafArea();
        }

        /// <summary>Calculate the actual leaf area</summary>
        public double CalcActualLeafArea(double dltStressedLAI)
        {
            return tilleringCalculator.CalcActualLeafArea(dltStressedLAI);
        }

        /// <summary> Reset Culms at start of the simulation </summary>
        [EventSubscribe("StartOfSimulation")]
        private void StartOfSim(object sender, EventArgs e)
        {
            // This can be null
            tilleringCalculator?.StartOfSim();
        }

        /// <summary>Called when crop is sowed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="sowingParameters">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowingParameters sowingParameters)
        {
            if (sowingParameters.Plant == plant && sowingParameters.TilleringMethod == 1)
            {
                tilleringCalculator ??= TilleringCalcsFactory.Create(
                        sowingParameters,
                        plant,
                        culms,
                        phenology,
                        leaf,
                        weather,
                        areaCalc,
                        tillerSdIntercept,
                        tillerSdSlope,
                        maxLAIForTillerAddition,
                        maxDailyTillerReduction,
                        tillerSlaBound
                    );

                tilleringCalculator.HandleOnPlantSowing(sowingParameters);
            }
        }
    }
}
