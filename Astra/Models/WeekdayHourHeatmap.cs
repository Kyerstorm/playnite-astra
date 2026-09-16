using System;
using System.Collections.Generic;

namespace Astra.Models
{
    /// <summary>One cell of the weekday x hour-bucket heatmap - a 3-hour window (e.g. 18:00-21:00) on
    /// one weekday, aggregated by session start time.</summary>
    public class WeekdayHourCell
    {
        public DayOfWeek DayOfWeek { get; set; }
        public int BucketStartHour { get; set; }
        public int BucketEndHour { get; set; }
        public long PlaytimeSeconds { get; set; }
        public int SessionCount { get; set; }

        /// <summary>0-1, relative to the busiest cell in this same result.</summary>
        public double Intensity { get; set; }

        public bool HasActivity => PlaytimeSeconds > 0;
    }

    /// <summary>Always exactly 7 x 8 = 56 cells: Monday-Sunday rows, eight 3-hour buckets
    /// (00-03, 03-06, ... 21-24) per row, in that fixed order - spec section 13.</summary>
    public class WeekdayHourHeatmap
    {
        public List<WeekdayHourCell> Cells { get; set; } = new List<WeekdayHourCell>();
    }
}
