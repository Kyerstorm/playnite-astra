namespace Astra.Models
{
    /// <summary>Output of BacklogBurndownService.Compute - the year-over-year trend in the
    /// percentage of owned games that have never been played. A DeltaPercentagePoints below
    /// zero means the backlog shrank (burned down); above zero means it grew.</summary>
    public class BacklogBurndownStats
    {
        public int OwnedGameCount { get; set; }
        public int UnplayedGameCount { get; set; }

        /// <summary>0-100. 0 when OwnedGameCount is 0 (never NaN).</summary>
        public double BacklogPercent { get; set; }

        /// <summary>Same calculation one year earlier, for the delta.</summary>
        public double PreviousYearBacklogPercent { get; set; }

        /// <summary>BacklogPercent - PreviousYearBacklogPercent. Negative = backlog shrank (burn-down); positive = grew.</summary>
        public double DeltaPercentagePoints { get; set; }
    }
}
