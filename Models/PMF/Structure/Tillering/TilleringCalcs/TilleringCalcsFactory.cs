using Models.Functions;
using Models.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.PMF.Struct;
using System;

namespace Models.PMF
{
    /// <summary>Tillering Calculations</summary>
    public static class TilleringCalcsFactory
    {
        /// <summary>The Rule of Thumb tillering method.</summary>
        public const int TILLERING_METHOD_RULE_OF_THUMB = -1;
        /// <summary>The Fixed tillering method.</summary>
        public const int TILLERING_METHOD_FIXED = 0;
        /// <summary>The Dynamic tillering method.</summary>
        public const int TILLERING_METHOD_DYNAMIC = 1;

        /// <summary>Creates the corresponding tillering calculations object.</summary>
        public static ITilleringCalcs Create(
            SowingParameters sowingParameters,
            Plant plant,
            LeafCulms culms,
            Phenology phenology,
            SorghumLeaf leaf,
            IClock clock,
            IWeather weather,
            IFunction areaCalc,
            IFunction tillerSdIntercept,
            IFunction tillerSdSlope,
            IFunction maxLAIForTillerAddition,
            IFunction maxDailyTillerReduction,
            IFunction tillerSlaBound
        )
        {
            switch (sowingParameters.TilleringMethod)
            {
                case TILLERING_METHOD_RULE_OF_THUMB:
                    var fertileTillerNumber = RuleOfThumbFTNGenerator.CalculateFtn(clock, weather, plant);
                    return new RuleOfThumbTilleringCalcs(plant, culms, phenology, leaf, weather, areaCalc, tillerSdIntercept, tillerSdSlope, maxLAIForTillerAddition, fertileTillerNumber);

                case TILLERING_METHOD_FIXED:
                    return new FixedTilleringCalcs(plant, culms, phenology, leaf, weather, areaCalc, tillerSdIntercept, tillerSdSlope, maxLAIForTillerAddition, sowingParameters.FTN);

                case TILLERING_METHOD_DYNAMIC:
                    return new DynamicTilleringCalcs(plant, culms, phenology, leaf, weather, areaCalc, tillerSdIntercept, tillerSdSlope, maxLAIForTillerAddition, maxDailyTillerReduction, tillerSlaBound);
            }

            throw new ArgumentException($"Invalid {nameof(sowingParameters.TilleringMethod)} {sowingParameters.TilleringMethod}");
        }
    }
}
