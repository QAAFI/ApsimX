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
    public class FixedTillering : Model, ITilleringMethod
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

        /// <summary>Culms on the leaf </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction areaCalc = null;

        /// <summary>Propoensity to Tiller Intercept </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction tillerSdIntercept = null;

        /// <summary>Propsenity to Tiller Slope </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction tillerSdSlope = null;

        /// <summary>LAI Value where tillers are no longer added </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction maxLAIForTillerAddition = null;

        /// <summary>Are we performing Rule Of Thumb tillering.</summary>
        private bool RuleOfThumbTilleringMethod { get; set; }

        /// <summary>If we are performing Fixed tillering then this will be the number of Fertile Tillers that was specified in the simulation.</summary>
        private double FixedTilleringFTN { get; set; }

        /// <summary>Actual Number of Fertile Tillers</summary>
        [JsonIgnore]
        public double FertileTillerNumber
        {
            get => tilleringCalculator?.FertileTillerNumber ?? 0.0;
            set
            {
                if (tilleringCalculator != null)
                {
                    tilleringCalculator.FertileTillerNumber = value;
                }
            }
        }

        /// <summary>Number of potential Fertile Tillers at harvest</summary>
        [JsonIgnore]
        public double CalculatedTillerNumber
        {
            get => tilleringCalculator?.CalculatedTillerNumber ?? 0.0;
        }

        [JsonIgnore]
        private FixedTilleringCalcs tilleringCalculator;

        /// <summary> Calculate number of leaves</summary>
        public double CalcLeafNumber()
        {
            return tilleringCalculator?.CalcLeafNumber(FixedTilleringFTN) ?? 0.0;
        }

        /// <summary>Calculate the potential leaf area</summary>
        public double CalcPotentialLeafArea()
        {
            return tilleringCalculator?.CalcPotentialLeafArea() ?? 0.0;
        }

        /// <summary>Calculate the actual leaf area</summary>
        public double CalcActualLeafArea(double dltStressedLAI)
        {
            return tilleringCalculator?.CalcActualLeafArea(dltStressedLAI) ?? 0.0;
        }

        /// <summary> Reset Culms at start of the simulation </summary>
        [EventSubscribe("StartOfSimulation")]
        private void StartOfSim(object sender, EventArgs e)
        {
            tilleringCalculator = new(
                plant,
                culms,
                phenology,
                leaf,
                weather,
                areaCalc,
                tillerSdIntercept,
                tillerSdSlope,
                maxLAIForTillerAddition
            );
        }

        /// <summary>Called when crop is sowed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowingParameters data)
        {
            if (data.Plant == plant)
            {
                tilleringCalculator.HandleOnPlantSowing(data);
                if (IsRuleOfThumbTilleringMethod(data.TilleringMethod))
                {
                    RuleOfThumbTilleringMethod = true;
                    //currentTillerNumber = TilleringCalculations.CalculateFtnRuleOfThumb(clock, plant, weather);
                }
                
                FixedTilleringFTN = data.FTN;
            }

            if (data.Plant == plant && data.TilleringMethod == 1)
            {
                tilleringCalculator.HandleOnPlantSowing(data);
            }
        }

        private static bool IsRuleOfThumbTilleringMethod(int tilleringMethod)
        {
            return tilleringMethod == -1;
        }
    }
}