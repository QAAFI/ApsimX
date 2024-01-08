using Models.PMF.Phen;

namespace Models.PMF.Struct
{
    /// <summary>
    /// Extension methods for the Phenology type.
    /// </summary>
    public static class PhenologyExtensions
    {
        private const string STAGE_FLAG_LEAF = "FlagLeaf";
        private const string STAGE_FLOWERING = "Flowering";
        private const string STAGE_END_JUVENILE = "EndJuvenile";
        private const string STAGE_START_GRAIN_FILL = "StartGrainFill";

        /// <summary>Is the current stage before flag leaf stage</summary>
        public static bool BeforeFlagLeafStage(this Phenology phenology)
        {
            return BeforePhase(phenology, STAGE_FLAG_LEAF);
        }

        /// <summary>Is the current stage before flowering stage</summary>
        public static bool BeforeFloweringStage(this Phenology phenology)
        {
            return BeforePhase(phenology, STAGE_FLOWERING);
        }

        /// <summary>Is the current stage before the end of juvenile stage</summary>
        public static bool BeforeEndJuvenileStage(this Phenology phenology)
        {
            return BeforePhase(phenology, STAGE_END_JUVENILE);
        }

        /// <summary>Is the current stage after the end of juvenile stage</summary>
        public static bool AfterEndJuvenileStage(this Phenology phenology)
        {
            return AfterPhase(phenology, STAGE_END_JUVENILE);
        }

        /// <summary>Is the current stage before the start of grain fill stage</summary>
        public static bool BeforeStartOfGrainFillStage(this Phenology phenology)
        {
            return BeforePhase(phenology, STAGE_START_GRAIN_FILL);
        }

        /// <summary>Is the current stage before the stage passed in.</summary>
        private static bool BeforePhase(Phenology phenology, string phaseName)
        {
            var phaseIndex = phenology.StartStagePhaseIndex(phaseName);
            return phenology.BeforePhase(phaseIndex);
        }

        /// <summary>Is the current stage after the stage passed in.</summary>
        private static bool AfterPhase(Phenology phenology, string phaseName)
        {
            var phaseIndex = phenology.StartStagePhaseIndex(phaseName);
            return phenology.BeyondPhase(phaseIndex);
        }
    }
}
