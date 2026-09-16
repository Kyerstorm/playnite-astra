namespace Astra.Models
{
    /// <summary>Session-level statistics (as opposed to bucketed playtime) for a date range - every
    /// number here is derived from individual session rows, not from TrendPoint buckets.</summary>
    public class SessionStats
    {
        public int TotalSessions { get; set; }
        public double AverageSessionSeconds { get; set; }
        public long LongestSessionSeconds { get; set; }
        public long ShortestSessionSeconds { get; set; }

        /// <summary>TotalSessions / distinct active days in the range. 0 when there are no active days.</summary>
        public double AverageSessionsPerActiveDay { get; set; }
    }
}
