using System;

namespace Astra.Models
{
    /// <summary>Dedicated Streaks section stats (spec section 15) - overlaps LongestStreakDays/
    /// CurrentStreakDays with GamingRhythmStats by design (both cards show them, at different
    /// altitudes), plus the week with the highest total playtime in the range.</summary>
    public class StreakStats
    {
        public int LongestStreakDays { get; set; }
        public int CurrentStreakDays { get; set; }

        /// <summary>Monday of the most active ISO week, or null when the range has no sessions.</summary>
        public DateTime? MostActiveWeekStart { get; set; }
        public long MostActiveWeekSeconds { get; set; }
    }
}
