using Models.Functions;
using Models.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.PMF.Struct;
using System;
using System.Linq;

namespace Models.PMF
{
    /// <summary>Rule Of Thumb Tillering Calculations</summary>
    [Serializable]
    public class RuleOfThumbTilleringCalcs : FixedTilleringCalcs
    {
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
            IFunction maxLAIForTillerAddition
        ) : base(plant, culms, phenology, leaf, weather, areaCalc, tillerSdIntercept, tillerSdSlope, maxLAIForTillerAddition, 0.0)
        {
        }

        #region Public Interface
        

        #endregion
    }
}
