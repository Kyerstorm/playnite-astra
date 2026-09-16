using System;

namespace Astra.Models
{
    /// <summary>One bucket in a trend chart - e.g. one day, one ISO week, one calendar month, one calendar year.
    /// Always present even when PlaytimeSeconds is 0, so charts render true chronological gaps rather than
    /// skipping inactive periods.</summary>
    public class TrendPoint
    {
        public DateTime PeriodStart { get; set; }

        /// <summary>Exclusive end of the period (same convention as AstraDatabase's range queries).</summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>Short chart-axis label, e.g. "Mar 18", "W12", "Mar", "2026" - formatting depends on Granularity.</summary>
        public string Label { get; set; }

        public long PlaytimeSeconds { get; set; }

        public int SessionCount { get; set; }

        /// <summary>Distinct games with at least one session in this bucket - powers the calendar
        /// heatmap's "games played" tooltip line and Game Rotation's "days with multiple games".</summary>
        public int ActiveGameCount { get; set; }
    }
}
