using System.Collections.Generic;

namespace Astra.Models
{
    /// <summary>One hour-of-day bucket (0-23), aggregated by session start time across the whole
    /// range - see spec section 12, "use session start time consistently ... do not attempt to
    /// reconstruct exact minute-by-minute activity."</summary>
    public class HourBucket
    {
        public int Hour { get; set; }
        public long PlaytimeSeconds { get; set; }
        public int SessionCount { get; set; }

        /// <summary>0-1, relative to the busiest hour - see WeekdayBucket.BarFraction.</summary>
        public double BarFraction { get; set; }
    }

    /// <summary>Always exactly 24 entries, hour 0 through hour 23 in that order.</summary>
    public class HourStats
    {
        public List<HourBucket> Hours { get; set; } = new List<HourBucket>();
    }
}
