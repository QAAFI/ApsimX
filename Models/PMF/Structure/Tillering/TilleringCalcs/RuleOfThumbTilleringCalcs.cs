using APSIM.Shared.Utilities;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.PMF.Struct;
using System;
using System.Text.Json.Serialization;

namespace Models.PMF
{
    /// <summary>Rule Of Thumb Tillering Calculations</summary>
    [Serializable]
    public class RuleOfThumbTilleringCalcs : FixedTilleringCalcs
    {
        /// <summary>Current Number of Tillers</summary>
        [JsonIgnore]
        public double CurrentTillerNumber { get; set; }

        /// <summary>Constuctor</summary>
        public RuleOfThumbTilleringCalcs(
            Plant plant,
            LeafCulms culms,
            Phenology phenology,
            SorghumLeaf leaf,
            IWeather weather,
            IFunction areaCalc,
            IFunction tillerSdIntercept,
            IFunction tillerSdSlope,
            IFunction maxLAIForTillerAddition,
            IClock clock
        ) : base(plant, culms, phenology, leaf, weather, areaCalc, tillerSdIntercept, tillerSdSlope, maxLAIForTillerAddition, RuleOfThumbFTNGenerator.CalculateFtn(clock, weather, plant))
        {
        }
    }
}
