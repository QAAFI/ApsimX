namespace Models.PMF
{
    /// <summary>Tillering Calculations interface</summary>
    public interface ITilleringCalcs
    {
        /// <summary>Called when a StartOfSim event is captured from the tillering classes</summary>
        public void StartOfSim();

        /// <summary>Called when an OnSowing event is captured from the tillering classes</summary>
        public void HandleOnPlantSowing(SowingParameters sowingParameters);

        /// <summary>Calculate number of leaves</summary>
        public double CalcLeafNumber();

        /// <summary>Calculate the actual leaf area</summary>
        public double CalcActualLeafArea(double dltStressedLAI);

        /// <summary>Calculates the potential leaf area.</summary>
        public double CalcPotentialLeafArea();

        /// <summary>Number of potential Fertile Tillers at harvest</summary>
        public double CalculatedTillerNumber { get; set; }        

        /// <summary>Actual Number of Fertile Tillers</summary>
        public double FertileTillerNumber { get; set; }
    }
}
