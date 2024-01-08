//using APSIM.Shared.Utilities;
//using Models.Functions;
//using Models.PMF.Interfaces;
//using Models.PMF.Phen;
//using Models.PMF.Struct;
//using System;

//namespace Models.PMF
//{
//    /// <summary>Common Tillering Calculations</summary>
//    public static class TilleringCalculations
//    {
//        private const int startThermalQuotientLeafNo = 3;
//        private const int endThermalQuotientLeafNo = 5;

//        /// <summary> Calculate number of leaves</summary>
//        public static double CalcLeafNumber(
//            Plant plant,
//            LeafCulms leafCulms,
//            Phenology phenology,
//            IFunction areaCalc,
//            double calculatedTillerNumber,
//            ref double radiationValues,
//            ref double temperatureValues
//        )
//        {
//            if (leafCulms.Culms?.Count == 0 ||
//                !plant.IsEmerged)
//            {
//                return 0.0;
//            }

//            if (PhenologyHelper.BeforeEndJuvenileStage(phenology))
//            {
//                // ThermalTime Targets to EndJuv are not known until the end of the Juvenile Phase
//                // FinalLeafNo is not known until the TT Target is known - meaning the potential leaf sizes aren't known
//                leafCulms.Culms.ForEach(c => c.UpdatePotentialLeafSizes(areaCalc as ICulmLeafArea));
//            }

//            var mainCulm = leafCulms.Culms[0];
//            var existingLeafNo = (int)Math.Floor(mainCulm.CurrentLeafNo);
//            var nLeaves = mainCulm.CurrentLeafNo;
//            var dltLeafNoMainCulm = 0.0;
//            leafCulms.dltLeafNo = dltLeafNoMainCulm;

//            if (PhenologyHelper.BeforeStartOfGrainFillStage(phenology))
//            {
//                // Calculate the leaf apperance on the main culm.
//                dltLeafNoMainCulm = CalcLeafAppearance(phenology, leafCulms, mainCulm);

//                // Now calculate the leaf apperance on all of the other culms.
//                for (int i = 1; i < leafCulms.Culms.Count; i++)
//                {
//                    CalcLeafAppearance(
//                        phenology, 
//                        leafCulms, 
//                        leafCulms.Culms[i]
//                    );
//                }
//            }

//            var newLeafNo = (int)Math.Floor(mainCulm.CurrentLeafNo);

//            if (nLeaves > startThermalQuotientLeafNo)
//            {
//                CalcTillers(calculatedTillerNumber, newLeafNo, existingLeafNo);
//                CalcTillerAppearance(newLeafNo, existingLeafNo);
//            }

//            return dltLeafNoMainCulm;
//        }

//        private void CalcTillers(
//            double calculatedTillerNumber,
//            int newLeaf, 
//            int currentLeaf,
//            ref double radiationValues,
//            ref double temperatureValues
//        )
//        {
//            if (CalculatedTillerNumber > 0.0) return;

//            // Up to L5 FE store PTQ. At L5 FE calculate tiller number (endThermalQuotientLeafNo).
//            // At L5 FE newLeaf = 6 and currentLeaf = 5
//            if (newLeaf >= startThermalQuotientLeafNo && currentLeaf < endThermalQuotientLeafNo)
//            {
//                radiationValues += metData.Radn;
//                temperatureValues += phenology.thermalTime.Value();

//                // L5 Fully Expanded
//                if (newLeaf == endThermalQuotientLeafNo)
//                {
//                    double PTQ = radiationValues / temperatureValues;
//                    CalcTillerNumber(PTQ);
//                    AddInitialTillers();
//                }
//            }
//        }

//        private static double CalcLeafAppearance(
//            Phenology phenology,
//            LeafCulms leafCulms, 
//            Culm mainCulm
//        )
//        {
//            var leavesRemaining = leafCulms.FinalLeafNo - mainCulm.CurrentLeafNo;
//            var leafAppearanceRate = leafCulms.getLeafAppearanceRate(leavesRemaining);
//            // if leaves are still growing, the cumulative number of phyllochrons or fully expanded leaves is calculated from thermal time for the day.
//            var dltLeafNo = MathUtilities.Bound(MathUtilities.Divide(phenology.thermalTime.Value(), leafAppearanceRate, 0), 0.0, leavesRemaining);

//            mainCulm.AddNewLeaf(dltLeafNo);

//            return dltLeafNo;
//        }
//    }
//}
