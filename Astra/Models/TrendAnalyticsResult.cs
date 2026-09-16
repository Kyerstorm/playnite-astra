using System.Collections.Generic;

namespace Astra.Models
{
    /// <summary>Composite output of PlaytimeInsightsService.Analyze() for one date range - every
    /// non-chart Trends analytics section reads from this same object, so they can never
    /// disagree about which sessions they're describing (see CLAUDE.md sync requirement, same
    /// principle as TrendResult for the chart). Built from a single bounded session fetch.</summary>
    public class TrendAnalyticsResult
    {
        public GamingRhythmStats Rhythm { get; set; }
        public SessionStats Sessions { get; set; }
        public WeekdayStats Weekday { get; set; }
        public List<SessionLengthBucket> SessionLengths { get; set; }
        public StreakStats Streaks { get; set; }
        public HourStats Hours { get; set; }
        public WeekdayHourHeatmap WeekdayHours { get; set; }
    }
}
