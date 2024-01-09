using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Functions;
using Models.GrazPlan;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.PMF.Struct;
using System;
using System.Collections.Generic;

namespace Models.PMF
{
    /// <summary>Common Tillering Calculations</summary>
    public static class TilleringCalculations
    {
        /// <summary>The start of the period during which average PTQ is calculated</summary>
        public const int START_THERMAL_QUOTIENT_LEAF_NO = 3;

        /// <summary>The end of the period during which average PTQ is calculated</summary>
        public const int END_THERMAL_QUOTIENT_LEAF_NO = 5;

        /// <summary>Calculates the fertile tiller number when using the Rule Of Thumb tillering method</summary>
        public static double CalculateFtnRuleOfThumb(
            IClock clock,
            Plant plant,
            IWeather weather
        )
        {
            // Estimate tillering given latitude, density, time of planting and
            // row configuration. This will be replaced with dynamic
            // calculations in the near future. Above latitude -25 is CQ, -25
            // to -29 is SQ, below is NNSW.
            double intercept;
            double slope;

            if (weather.Latitude > -12.5 || weather.Latitude < -38.0)
            {
                // Unknown region.
                throw new Exception($"Unable to estimate number of tillers at latitude {weather.Latitude}");
            }

            if (weather.Latitude > -25.0)
            {
                // Central Queensland.
                if (clock.Today.DayOfYear < 319 && clock.Today.DayOfYear > 182)
                {
                    // Between 1 July and 15 November.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 0.5786; 
                        slope = -0.0521;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 0.8786; 
                        slope = -0.0696;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 1.1786; 
                        slope = -0.0871;
                    }
                }
                else
                {
                    // After 15 November.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 0.4786; 
                        slope = -0.0421;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5)
                        intercept = 0.6393; 
                        slope = -0.0486;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 0.8000;
                        slope = -0.0550;
                    }
                }
            }
            else if (weather.Latitude > -29.0)
            {
                // South Queensland.
                if (clock.Today.DayOfYear < 319 && clock.Today.DayOfYear > 182)
                {
                    // Between 1 July and 15 November.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double  (2.0).
                        intercept = 1.1571; 
                        slope = -0.1043;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 1.7571; 
                        slope = -0.1393;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 2.3571; 
                        slope = -0.1743;
                    }
                }
                else
                {
                    // After 15 November.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 0.6786; 
                        slope = -0.0621;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 1.1679; 
                        slope = -0.0957;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 1.6571; 
                        slope = -0.1293;
                    }
                }
            }
            else
            {
                // Northern NSW.
                if (clock.Today.DayOfYear < 319 && clock.Today.DayOfYear > 182)
                {
                    //  Between 1 July and 15 November.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 1.3571; 
                        slope = -0.1243;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 2.2357; 
                        slope = -0.1814;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 3.1143; 
                        slope = -0.2386;
                    }
                }
                else if (clock.Today.DayOfYear > 349 || clock.Today.DayOfYear < 182)
                {
                    // Between 15 December and 1 July.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 0.4000; 
                        slope = -0.0400;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 1.0571; 
                        slope = -0.0943;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 1.7143;
                        slope = -0.1486;
                    }
                }
                else
                {
                    // Between 15 November and 15 December.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 0.8786; 
                        slope = -0.0821;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 1.6464; 
                        slope = -0.1379;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 2.4143;
                        slope = -0.1936;
                    }
                }
            }

            var calculatedFTN =  Math.Max(slope * plant.SowingData.Population + intercept, 0);
            return calculatedFTN;
        }

        ///// <summary>Calculates the leaf number</summary>
        //public static double CalcLeafNumber(
        //    Weather weather,
        //    Phenology phenology,
        //    Plant plant,
        //    LeafCulms leafCulms,
        //    IFunction areaCalc,
        //    IFunction tillerSdIntercept,
        //    IFunction tillerSdSlope,
        //    double calculatedTillerNumber,
        //    ref List<int> tillerOrder,
        //    ref double currentTillerNumber,
        //    ref double radiationValues,
        //    ref double temperatureValues
        //)
        //{
        //    if (leafCulms.Culms?.Count == 0) return 0.0;
        //    if (!plant.IsEmerged) return 0.0;

        //    if (phenology.BeforeEndJuvenileStage())
        //    {
        //        // ThermalTime Targets to EndJuv are not known until the end of the Juvenile Phase
        //        // FinalLeafNo is not known until the TT Target is known - meaning the potential leaf sizes aren't known
        //        leafCulms.Culms.ForEach(c => c.UpdatePotentialLeafSizes(areaCalc as ICulmLeafArea));
        //    }

        //    var mainCulm = leafCulms.Culms[0];
        //    var existingLeafNo = (int)Math.Floor(mainCulm.CurrentLeafNo);
        //    var nLeaves = mainCulm.CurrentLeafNo;
        //    var dltLeafNoMainCulm = 0.0;
        //    leafCulms.dltLeafNo = dltLeafNoMainCulm;

        //    if (phenology.BeforeStartOfGrainFillStage())
        //    {
        //        // Calculate the leaf apperance on the main culm.
        //        dltLeafNoMainCulm = CalcLeafAppearance(phenology, leafCulms, mainCulm);

        //        // Now calculate the leaf apperance on all of the other culms.
        //        for (int i = 1; i < leafCulms.Culms.Count; i++)
        //        {
        //            CalcLeafAppearance(phenology, leafCulms, leafCulms.Culms[i]);
        //        }
        //    }

        //    var newLeafNo = (int)Math.Floor(mainCulm.CurrentLeafNo);

        //    if (nLeaves > START_THERMAL_QUOTIENT_LEAF_NO)
        //    {
        //        CalcTillers(
        //            weather,
        //            phenology,
        //            leafCulms,
        //            areaCalc,
        //            tillerSdIntercept,
        //            tillerSdSlope,
        //            calculatedTillerNumber,
        //            newLeafNo, 
        //            existingLeafNo,
        //            ref tillerOrder,
        //            ref currentTillerNumber,
        //            ref radiationValues,
        //            ref temperatureValues
        //        );
        //        CalcTillerAppearance(newLeafNo, existingLeafNo);
        //    }

        //    return dltLeafNoMainCulm;
        //}

        //private static void CalcTillerAppearance(
        //    Weather weather,
        //    Phenology phenology,
        //    Plant plant,
        //    SorghumLeaf leaf,
        //    LeafCulms leafCulms,
        //    IFunction areaCalc,
        //    IFunction tillerSdIntercept,
        //    IFunction tillerSdSlope,
        //    double calculatedTillerNumber,
        //    ref List<int> tillerOrder,
        //    ref double currentTillerNumber,
        //    ref double radiationValues,
        //    ref double temperatureValues,
        //    int newLeaf, 
        //    int currentLeaf
        //)
        //{
        //    // Each time a leaf becomes fully expanded starting at 5 see if a tiller should be initiated.
        //    // When a leaf is fully expanded a tiller can be initiated at the axil 3 leaves less
        //    // So at L5 FE (newLeaf = 6, currentLeaf = 5) a Tiller might be at axil 2. i.e. a T2 

        //    // Add any new tillers and then calc each tiller in turn. Add a tiller if:
        //    // 1. There are more tillers to add.
        //    // 2. linearLAI < maxLAIForTillerAddition
        //    // 3. A leaf has fully expanded.  (newLeaf >= 6, newLeaf > currentLeaf)
        //    // 4. there should be a tiller at that node. (Check tillerOrder)

        //    var tillersAdded = leafCulms.Culms.Count - 1;
        //    linearLAI = TilleringCalculations.CalcLinearLAI(leaf, population, plantsPerMetre);

        //    if (newLeaf >= 5 &&
        //        newLeaf > currentLeaf &&
        //        CalculatedTillerNumber > tillersAdded &&
        //        linearLAI < maxLAIForTillerAddition.Value()
        //    )
        //    {
        //        // Axil = currentLeaf - 3
        //        int newNodeNumber = newLeaf - 3;
        //        if (tillerOrder.Contains(newNodeNumber))
        //        {
        //            var fractionToAdd = Math.Min(1.0, CalculatedTillerNumber - tillersAdded);

        //            DltTillerNumber = fractionToAdd;
        //            currentTillerNumber += fractionToAdd;

        //            TilleringCalculations.InitiateTiller(
        //                leafCulms,
        //                areaCalc,
        //                currentTillerNumber,
        //                newNodeNumber,
        //                fractionToAdd,
        //                1
        //            );
        //        }
        //    }
        //}

        //private static void CalcTillers(
        //    Weather weather,
        //    Phenology phenology,
        //    LeafCulms leafCulms,
        //    IFunction areaCalc,
        //    IFunction tillerSdIntercept,
        //    IFunction tillerSdSlope,
        //    double calculatedTillerNumber,
        //    int newLeaf, 
        //    int currentLeaf,
        //    ref List<int> tillerOrder,
        //    ref double currentTillerNumber,
        //    ref double radiationValues,
        //    ref double temperatureValues
        //)
        //{
        //    if (calculatedTillerNumber > 0.0) return;

        //    // Up to L5 FE store PTQ. At L5 FE calculate tiller number (endThermalQuotientLeafNo).
        //    // At L5 FE newLeaf = 6 and currentLeaf = 5
        //    if (newLeaf >= START_THERMAL_QUOTIENT_LEAF_NO &&
        //        currentLeaf < END_THERMAL_QUOTIENT_LEAF_NO)
        //    {
        //        radiationValues += weather.Radn;
        //        temperatureValues += phenology.thermalTime.Value();

        //        // L5 Fully Expanded
        //        if (newLeaf == END_THERMAL_QUOTIENT_LEAF_NO)
        //        {
        //            double PTQ = radiationValues / temperatureValues;
        //            calculatedTillerNumber = CalcTillerNumber(leafCulms, areaCalc, tillerSdIntercept, tillerSdSlope, PTQ);
        //            AddInitialTillers(calculatedTillerNumber, leafCulms, areaCalc, ref tillerOrder, ref currentTillerNumber);
        //        }
        //    }
        //}

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
        public static List<int> CalculateTillerOrder(
            double calculatedTillerNumber
        )
        {
            if (calculatedTillerNumber <= 0) return new();

            // At leaf 5 fully expanded only initialize T1 with 2 leaves if present.

            int nTillers = (int)Math.Ceiling(calculatedTillerNumber);
            if (nTillers <= 0) return new();

            List<int> calculatedTillerOrder = new();

            if (nTillers < 3) calculatedTillerOrder.Add(3);
            if (nTillers == 2) calculatedTillerOrder.Add(4);
            if (nTillers == 3)
            {
                calculatedTillerOrder.Add(2);
                calculatedTillerOrder.Add(3);
                calculatedTillerOrder.Add(4);
            }
            if (nTillers > 3)
            {
                for (int i = 1; i <= nTillers; i++)
                {
                    calculatedTillerOrder.Add(i);
                }
            }

            return calculatedTillerOrder;
        }

        /// <summary>Calculates the Linear LAI</summary>
        public static double CalcLinearLAI(
            SorghumLeaf leaf,
            double population,
            double plantsPerMetre
        )
        {
            // Leaf area of one plant.
            var tpla = (leaf.LAI + leaf.SenescedLai) / population * 10000;
            var linearLAI = plantsPerMetre * tpla / 10000.0;
            return linearLAI;
        }

        /// <summary>Calculate SLA for leafa rea including potential new growth - stressess effect</summary>
        public static double CalcCurrentSLA(
            SorghumLeaf leaf,
            double stressedLAI
        )
        {
            double dmGreen = leaf.Live.Wt;
            double dltDmGreen = leaf.potentialDMAllocation.Structural;

            if (dmGreen + dltDmGreen <= 0.0) return 0.0;

            // (cm^2/g)
            return (leaf.LAI + stressedLAI) / (dmGreen + dltDmGreen) * 10000;
        }

        /// <summary>Calculates the carbon limitation.</summary>
        public static double CalcCarbonLimitation(
            SorghumLeaf leaf,
            LeafCulms leafCulms,
            double dltStressedLAI
        )
        {
            double dltDmGreen = leaf.potentialDMAllocation.Structural;
            if (dltDmGreen <= 0.001) return 1.0;

            var mainCulm = leafCulms.Culms[0];

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

        /// <summary>Calculates the leaf appearance.</summary>
        public static double CalcLeafAppearance(
            Phenology phenology,
            LeafCulms leafCulms,
            Culm culm
        )
        {
            var leavesRemaining = leafCulms.FinalLeafNo - culm.CurrentLeafNo;
            var leafAppearanceRate = leafCulms.getLeafAppearanceRate(leavesRemaining);
            // If leaves are still growing, the cumulative number of phyllochrons or fully expanded leaves is calculated from thermal time for the day.
            var dltLeafNo = MathUtilities.Bound(MathUtilities.Divide(phenology.thermalTime.Value(), leafAppearanceRate, 0), 0.0, leavesRemaining);

            culm.AddNewLeaf(dltLeafNo);

            return dltLeafNo;
        }

        /// <summary>Calculates the potential leaf area.</summary>
        public static double CalcPotentialLeafArea(
            Phenology phenology,
            LeafCulms leafCulms,
            IFunction areaCalc
        )
        {
            leafCulms.Culms.ForEach(c => c.DltLAI = 0);
            if (phenology.BeforeFloweringStage())
            {
                return areaCalc.Value();
            }
            return 0.0;
        }

        /// <summary>Adds the initial tillers.</summary>
        public static void AddInitialTillers(
            double calculatedTillerNumber,
            LeafCulms leafCulms,
            IFunction areaCalc,
            ref List<int> tillerOrder,
            ref double currentTillerNumber)
        {
            tillerOrder = CalculateTillerOrder(calculatedTillerNumber);

            if (calculatedTillerNumber <= 0) return;

            int nTillers = (int)Math.Ceiling(calculatedTillerNumber);

            if (nTillers > 3)
            {
                InitiateTiller(
                    leafCulms,
                    areaCalc,
                    currentTillerNumber,
                    1,
                    1,
                    2
                );

                currentTillerNumber = 1;
            }
        }

        /// <summary>Calculates the potential leaf area.</summary>
        public static void InitiateTiller(
            LeafCulms leafCulms,
            IFunction areaCalc,
            double currentTillerNumber,
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
                VertAdjValue = leafCulms.MaxVerticalTillerAdjustment.Value() + (currentTillerNumber * leafCulms.VerticalTillerAdjustment.Value()),
                Proportion = fractionToAdd,
                FinalLeafNo = leafCulms.Culms[0].FinalLeafNo
            };
            newCulm.UpdatePotentialLeafSizes(areaCalc as ICulmLeafArea);
            leafCulms.Culms.Add(newCulm);
        }

        /// <summary>Calculates the tiller number.</summary>
        public static double CalcTillerNumber(
            LeafCulms leafCulms,
            IFunction areaCalc,
            IFunction tillerSdIntercept,
            IFunction tillerSdSlope,
            double PTQ,
            double fixedTilleringFTN = 0.0
        )
        {
            // The final tiller number (Ftn) is calculated after the full appearance of LeafNo 5 - when leaf 6 emerges.
            // Calc Supply = R/oCd * LA5 * Phy5
            var areaMethod = areaCalc as ICulmLeafArea;
            var mainCulm = leafCulms.Culms[0];
            double L5Area = areaMethod.CalculateIndividualLeafArea(5, mainCulm);
            double L9Area = areaMethod.CalculateIndividualLeafArea(9, mainCulm);

            double Phy5 = leafCulms.getLeafAppearanceRate(leafCulms.FinalLeafNo - leafCulms.Culms[0].CurrentLeafNo);

            // Calc Demand = LA9 - LA5
            var demand = L9Area - L5Area;
            var supply = PTQ * L5Area * Phy5;
            var supplyDemandRatio = MathUtilities.Divide(supply, demand, 0);           
            // Calculate the tiller number using the intercept and slope values.
            var calculatedTillerNumber = tillerSdIntercept.Value() + tillerSdSlope.Value() * supplyDemandRatio;

            // If we've got a fixed tillering FTN, then we need to limit it based on this.
            if (fixedTilleringFTN > 0.0)
            {
                calculatedTillerNumber = Math.Min(fixedTilleringFTN, calculatedTillerNumber);
            }

            calculatedTillerNumber = Math.Max(calculatedTillerNumber, 0.0);

            return calculatedTillerNumber;
        }
    }
}
