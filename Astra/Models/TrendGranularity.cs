namespace Astra.Models
{
    /// <summary>Aggregation level for a playtime trend chart. Drives how TrendAggregationService buckets sessions.</summary>
    public enum TrendGranularity
    {
        Day,
        Week,
        Month,
        Year
    }
}
