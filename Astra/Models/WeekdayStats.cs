using System;
using System.Collections.Generic;

namespace Astra.Models
{
    /// <summary>One calendar weekday's total playtime within a date range.</summary>
    public class WeekdayBucket
    {
        public DayOfWeek DayOfWeek { get; set; }
        public long PlaytimeSeconds { get; set; }
        public int SessionCount { get; set; }

        /// <summary>0-1, relative to the busiest day in this same result - a presentation convenience
        /// so the horizontal-bar view doesn't need its own max-finding logic, not a business metric.</summary>
        public double BarFraction { get; set; }
    }

    /// <summary>Always exactly 7 entries, Monday through Sunday in that fixed order - this represents
    /// a calendar pattern, not a leaderboard, so it is never sorted by value (spec section 11).</summary>
    public class WeekdayStats
    {
        public List<WeekdayBucket> Days { get; set; } = new List<WeekdayBucket>();
    }
}
