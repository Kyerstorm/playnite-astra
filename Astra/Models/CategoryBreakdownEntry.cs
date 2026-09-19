namespace Astra.Models
{
    /// <summary>One row of a genre/platform playtime breakdown - same Label/value/Percentage/BarFraction
    /// shape already used by SessionLengthBucket/WeekdayBucket, so the bar-list XAML template is
    /// identical across all of them.</summary>
    public class CategoryBreakdownEntry
    {
        public string Label { get; set; }
        public long PlaytimeSeconds { get; set; }

        /// <summary>0-100, relative to the sum of all entries in this same breakdown (not the year's
        /// total playtime - see RecapAggregator's BuildCategoryBreakdown for the multi-genre trade-off).</summary>
        public double Percentage { get; set; }

        /// <summary>0-1, relative to the top entry in this same breakdown.</summary>
        public double BarFraction { get; set; }
    }
}
