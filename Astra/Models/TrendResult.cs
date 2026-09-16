using System;
using System.Collections.Generic;

namespace Astra.Models
{
    /// <summary>Output of TrendAggregationService for one granularity + date range: the bucketed points plus
    /// summary statistics, so Home's mini-chart and the Trends page's full chart/cards can both bind to the
    /// same shape without recomputing anything themselves.</summary>
    public class TrendResult
    {
        public TrendGranularity Granularity { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public long TotalPlaytimeSeconds { get; set; }
        public double AveragePlaytimeSeconds { get; set; }
        public int ActivePeriods { get; set; }
        public int TotalPeriods { get; set; }

        public List<TrendPoint> Points { get; set; } = new List<TrendPoint>();

        public bool HasActivity => TotalPlaytimeSeconds > 0;
    }
}
