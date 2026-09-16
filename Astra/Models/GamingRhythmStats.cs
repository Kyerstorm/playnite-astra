using System;

namespace Astra.Models
{
    /// <summary>Factual, non-judgmental gaming-rhythm statistics for a date range - which calendar
    /// weekday/month accumulates the most playtime, and the user's current/longest run of consecutive
    /// active days within that same range. See PlaytimeInsightsService for how these are derived.</summary>
    public class GamingRhythmStats
    {
        /// <summary>Null when the range has no sessions at all.</summary>
        public DayOfWeek? BusiestDayOfWeek { get; set; }
        public long BusiestDayOfWeekSeconds { get; set; }

        /// <summary>1-12, or null when the range has no sessions. Aggregated across all years present
        /// in the range - "September" totals every September in the selected window, not one specific year.</summary>
        public int? BusiestMonth { get; set; }
        public long BusiestMonthSeconds { get; set; }

        public int LongestStreakDays { get; set; }
        public int CurrentStreakDays { get; set; }
    }
}
