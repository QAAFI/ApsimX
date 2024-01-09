using Models.Functions;
using Models.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.PMF.Struct;
using System;
using System.Linq;

namespace Models.PMF
{
    /// <summary>Dynamic Tillering Calculations</summary>
    [Serializable]
    public class DynamicTilleringCalcs : FixedTilleringCalcs
    {
        /// <summary> LAI Value where tillers are no longer added </summary>
        private readonly IFunction maxDailyTillerReduction = null;

        /// <summary> LAI Value where tillers are no longer added </summary>
        private readonly IFunction tillerSlaBound = null;

        /// <summary>Constuctor</summary>
        public DynamicTilleringCalcs(
            Plant plant,
            LeafCulms culms,
            Phenology phenology,
            SorghumLeaf leaf,
            IWeather weather,
            IFunction areaCalc,
            IFunction tillerSdIntercept,
            IFunction tillerSdSlope,
            IFunction maxLAIForTillerAddition,
            IFunction maxDailyTillerReduction,
            IFunction tillerSlaBound
        ) : base(plant, culms, phenology, leaf, weather, areaCalc, tillerSdIntercept, tillerSdSlope, maxLAIForTillerAddition)
        {
            this.maxDailyTillerReduction = maxDailyTillerReduction;
            this.tillerSlaBound = tillerSlaBound;
        }

        #region Public Interface

        /// <summary>Calculate the actual leaf area but also take into consideration tiller cessation.</summary>
        public override double CalcActualLeafArea(double dltStressedLAI)
        {
            var mainCulm = culms.Culms.FirstOrDefault();

            if (mainCulm != null &&
                phenology.AfterEndJuvenileStage() &&
                CalculatedTillerNumber > 0.0 &&
                mainCulm.CurrentLeafNo < mainCulm.PositionOfLargestLeaf
            )
            {
                CalculateTillerCessation(dltStressedLAI);
            }

            // Recalculate todays LAI
            var dltStressedLAI2 = 0.0;
            foreach (var culm in culms.Culms)
            {
                dltStressedLAI2 += culm.DltLAI;
            }

            double laiSlaReductionFraction = CalcCarbonLimitation(dltStressedLAI2);
            var dltLAI = Math.Max(dltStressedLAI2 * laiSlaReductionFraction, 0.0);

            // Apply to each culm
            if (laiSlaReductionFraction < 1.0)
            {
                ReduceAllTillersProportionately(laiSlaReductionFraction);
            }

            culms.Culms.ForEach(c => c.TotalLAI += c.DltStressedLAI);

            return dltLAI;
        }

        #endregion

        #region Private Interface

        private void CalculateTillerCessation(double dltStressedLAI)
        {
            bool moreToAdd =
                FertileTillerNumber < CalculatedTillerNumber &&
                LinearLAI < maxLAIForTillerAddition.Value();

            var tillerLaiToReduce = CalcCeaseTillerSignal(dltStressedLAI);
            double nLeaves = culms.Culms[0].CurrentLeafNo;

            if (nLeaves < 8 || moreToAdd || tillerLaiToReduce < 0.00001) return;

            double maxTillerLoss = maxDailyTillerReduction.Value();
            double accProportion = 0.0;
            double tillerLaiLeftToReduce = tillerLaiToReduce;

            for (var culmIndex = culms.Culms.Count - 1; culmIndex >= 1; culmIndex--)
            {
                if (accProportion < maxTillerLoss && tillerLaiLeftToReduce > 0)
                {
                    var culm = culms.Culms[culmIndex];

                    double tillerLAI = culm.TotalLAI;
                    double tillerProportion = culm.Proportion;

                    if (tillerProportion > 0.0 && tillerLAI > 0.0)
                    {
                        // Use the amount of LAI past the target as an indicator of how much of the tiller
                        // to remove which will affect tomorrow's growth - up to the maxTillerLoss
                        double propn = Math.Max(
                            0.0,
                            Math.Min(maxTillerLoss - accProportion, tillerLaiLeftToReduce / tillerLAI)
                        );

                        accProportion += propn;
                        tillerLaiLeftToReduce -= propn * tillerLAI;
                        double remainingProportion = Math.Max(0.0, culm.Proportion - propn);
                        // Can't increase the proportion
                        culm.Proportion = remainingProportion;

                        culm.DltLAI -= propn * tillerLAI;
                    }
                }

                if (!(tillerLaiLeftToReduce > 0) || accProportion >= maxTillerLoss) break;
            }

            FertileTillerNumber = 0;
            culms.Culms.ForEach(c => FertileTillerNumber += c.Proportion);
            FertileTillerNumber -= 1;
        }

        private double CalcCeaseTillerSignal(double dltStressedLAI)
        {
            var mainCulm = culms.Culms.FirstOrDefault();

            // Calculate sla target that is below the actual SLA - so as the leaves gets thinner it signals to the tillers to cease growing further
            // max SLA (thinnest leaf) possible using Reeves (1960's Kansas) SLA = 429.72 - 18.158 * LeafNo
            double nLeaves = mainCulm.CurrentLeafNo;
            var maxSLA = 429.72 - 18.158 * (nLeaves);
            // sla bound vary 30 - 40%
            maxSLA *= ((100 - tillerSlaBound.Value()) / 100.0);
            maxSLA = Math.Min(400, maxSLA);
            maxSLA = Math.Max(150, maxSLA);

            double dmGreen = leaf.Live.Wt;
            double dltDmGreen = leaf.potentialDMAllocation.Structural;

            // Calc how much LAI we need to remove to get back to the SLA target line.
            // This is done by reducing the proportion of tiller area.
            var maxLaiTarget = maxSLA * (dmGreen + dltDmGreen) / 10000;
            return Math.Max(leaf.LAI + dltStressedLAI - maxLaiTarget, 0);
        }

        private void ReduceAllTillersProportionately(double laiReduction)
        {
            if (laiReduction <= 0.0) return;

            double totalDltLeaf = culms.Culms.Sum(c => c.DltStressedLAI);
            if (totalDltLeaf <= 0.0) return;

            // Reduce new leaf growth proportionally across all culms
            // not reducing the number of tillers at this stage
            culms.Culms.ForEach(
                c =>
                c.DltStressedLAI -= Math.Max(
                    c.DltStressedLAI / totalDltLeaf * laiReduction,
                    0.0
                )
            );
        }

        #endregion
    }
}
