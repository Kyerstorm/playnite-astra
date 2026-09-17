using System;

namespace Astra.Models
{
    /// <summary>One selectable single period in the Trends page's period dropdown - e.g. "17 Sep 2026"
    /// for Day, "8 Sep - 14 Sep 2026" for Week, "September 2026" for Month, "2026" for Year. Replaces
    /// the earlier "Last N days/weeks/years" rolling-window presets: each tab now shows exactly one
    /// period of its own length, chosen here, rather than a trend across several of them.</summary>
    public class TrendPeriodOption
    {
        public string Label { get; set; }
        public DateTime PeriodStart { get; set; }

        /// <summary>Exclusive end, same convention as everywhere else (AstraDatabase range queries,
        /// TrendPoint.PeriodEnd).</summary>
        public DateTime PeriodEnd { get; set; }
    }
}
