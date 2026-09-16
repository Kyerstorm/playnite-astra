namespace Astra.Models
{
    /// <summary>One session-duration bucket ("1-2 hours") with how many sessions in the range fall
    /// into it. Boundaries are fixed and inclusive-low/exclusive-high, see
    /// PlaytimeInsightsService.SessionLengthBucketBoundaries.</summary>
    public class SessionLengthBucket
    {
        public string Label { get; set; }
        public int SessionCount { get; set; }

        /// <summary>0-100. 0 when the range has no sessions at all (never NaN).</summary>
        public double Percentage { get; set; }

        /// <summary>0-1, relative to the busiest bucket - see WeekdayBucket.BarFraction for why this
        /// lives on the model instead of in the view.</summary>
        public double BarFraction { get; set; }
    }
}
