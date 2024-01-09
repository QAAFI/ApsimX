using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <summary>
        /// Link to clock (used for FTN calculations at time of sowing).
        /// </summary>
        [Link]
        private readonly IClock clock = null;

        /// <summary>
        /// Link to weather.
        /// </summary>
        [Link]
        private readonly IWeather weather = null;

        /// <summary>The parent Plant</summary>
        [Link]
        private readonly Plant plant = null;

        /// <summary> Culms on the leaf </summary>
        [Link]
        private readonly LeafCulms culms = null;

        /// <summary>The parent tilering class</summary>
        [Link]
        private readonly Phenology phenology = null;

        /// <summary>The parent tilering class</summary>
        [Link]
        private readonly SorghumLeaf leaf = null;

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

        /// <summary>Number of potential Fertile Tillers at harvest</summary>
        [JsonIgnore]
        public double CalculatedTillerNumber { get; set; }

        /// <summary>Current Number of Tillers</summary>
        [JsonIgnore]
        public double DltTillerNumber { get; set; }

        /// <summary>Actual Number of Fertile Tillers</summary>
        [JsonIgnore]
        public double FertileTillerNumber
        {
            get => currentTillerNumber;
            set => currentTillerNumber = value;
        }

        private double currentTillerNumber;
        private double fixedTilleringFTN = 0.0;
        private double plantsPerMetre;
        private double population;
        private double radiationValues = 0.0;
        private double temperatureValues = 0.0;
        private bool isRuleOfThumbTilleringMethod = false;
        private List<int> tillerOrder = new();

        /// <summary> Calculate number of leaves</summary>
        public double CalcLeafNumber()
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
                dltLeafNoMainCulm = TilleringCalculations.CalcLeafAppearance(phenology, culms, mainCulm);

                // Now calculate the leaf apperance on all of the other culms.
                for (int i = 1; i < culms.Culms.Count; i++)
                {
                    TilleringCalculations.CalcLeafAppearance(phenology, culms, culms.Culms[i]);
                }
            }

            var newLeafNo = (int)Math.Floor(mainCulm.CurrentLeafNo);

            if (nLeaves > TilleringCalculations.START_THERMAL_QUOTIENT_LEAF_NO)
            {
                CalcTillers(newLeafNo, existingLeafNo);
                CalcTillerAppearance(newLeafNo, existingLeafNo);
            }

            return dltLeafNoMainCulm;
        }

        /// <summary>Calculate the potential leaf area</summary>
        public double CalcPotentialLeafArea()
        {
            return TilleringCalculations.CalcPotentialLeafArea(phenology, culms, areaCalc);
        }

        /// <summary> calculate the actual leaf area</summary>
        public double CalcActualLeafArea(double dltStressedLAI)
        {
            var mainCulm = culms.Culms.FirstOrDefault();

            double laiSlaReductionFraction = TilleringCalculations.CalcCarbonLimitation(leaf, culms, dltStressedLAI);
            var dltLAI = Math.Max(dltStressedLAI * laiSlaReductionFraction, 0.0);

            culms.Culms.ForEach(c => c.TotalLAI += c.DltStressedLAI);

            return dltLAI;
        }

        private void CalcTillers(int newLeaf, int currentLeaf)
        {
            if (CalculatedTillerNumber > 0.0) return;

            // Up to L5 FE store PTQ. At L5 FE calculate tiller number (endThermalQuotientLeafNo).
            // At L5 FE newLeaf = 6 and currentLeaf = 5
            if (newLeaf >= TilleringCalculations.START_THERMAL_QUOTIENT_LEAF_NO && 
                currentLeaf < TilleringCalculations.END_THERMAL_QUOTIENT_LEAF_NO)
            {
                radiationValues += weather.Radn;
                temperatureValues += phenology.thermalTime.Value();

                // L5 Fully Expanded
                if (newLeaf == TilleringCalculations.END_THERMAL_QUOTIENT_LEAF_NO)
                {
                    double PTQ = radiationValues / temperatureValues;
                    CalculatedTillerNumber = TilleringCalculations.CalcTillerNumber(culms, areaCalc, tillerSdIntercept, tillerSdSlope, PTQ, fixedTilleringFTN);
                    TilleringCalculations.AddInitialTillers(CalculatedTillerNumber, culms, areaCalc, ref tillerOrder, ref currentTillerNumber);
                }
            }
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
            double lLAI = TilleringCalculations.CalcLinearLAI(leaf, population, plantsPerMetre);

            if (newLeaf >= 5 &&
                newLeaf > currentLeaf &&
                CalculatedTillerNumber > tillersAdded &&
                lLAI < maxLAIForTillerAddition.Value()
            )
            {
                // Axil = currentLeaf - 3
                int newNodeNumber = newLeaf - 3;
                if (tillerOrder.Contains(newNodeNumber))
                {
                    var fractionToAdd = Math.Min(1.0, CalculatedTillerNumber - tillersAdded);

                    DltTillerNumber = fractionToAdd;
                    currentTillerNumber += fractionToAdd;

                    TilleringCalculations.InitiateTiller(
                        culms,
                        areaCalc,
                        currentTillerNumber,
                        newNodeNumber, 
                        fractionToAdd, 
                        1
                    );
                }
            }
        }

        /// <summary> Reset Culms at start of the simulation </summary>
        [EventSubscribe("StartOfSimulation")]
        private void StartOfSim(object sender, EventArgs e)
        {
            currentTillerNumber = 0.0;
            CalculatedTillerNumber = 0.0;
            DltTillerNumber = 0.0;
        }

        /// <summary>Called when crop is sowed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowingParameters data)
        {
            if (data.Plant == plant)
            {
                currentTillerNumber = 0.0;

                isRuleOfThumbTilleringMethod = IsRuleOfThumbTilleringMethod(data.TilleringMethod);

                if (isRuleOfThumbTilleringMethod)
                {
                    currentTillerNumber = TilleringCalculations.CalculateFtnRuleOfThumb(clock, plant, weather);
                }

                population = data.Population;
                plantsPerMetre = data.Population * data.RowSpacing / 1000.0 * data.SkipDensityScale;                
                CalculatedTillerNumber = 0.0;
                fixedTilleringFTN = data.FTN;

                radiationValues = 0.0;
                temperatureValues = 0.0;
            }
        }

        private static bool IsRuleOfThumbTilleringMethod(int tilleringMethod)
        {
            return tilleringMethod == -1;
        }
    }
}
