namespace Astra.Models
{
    /// <summary>Aggregation level for a playtime trend chart. Drives how TrendAggregationService buckets sessions.
    /// Day/Week/Month/Year are the four Trends page tabs; Hour exists only as the main chart's internal bucket
    /// size when a single day is selected (TrendsViewModel.ChartBucketGranularity) - it is never a selectable tab.</summary>
    public enum TrendGranularity
    {
        Hour,
        Day,
        Week,
        Month,
        Year
    }
}
