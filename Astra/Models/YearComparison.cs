using System.Collections.Generic;

namespace Astra.Models
{
    /// <summary>One year's summary numbers for the Year-over-Year comparison. Purely numerical -
    /// no red/green, no "better/worse" framing anywhere this is displayed (spec section 16).</summary>
    public class YearComparisonPeriod
    {
        public int Year { get; set; }
        public long PlaytimeSeconds { get; set; }
        public int SessionCount { get; set; }
        public int ActiveDays { get; set; }
        public int GamesTouched { get; set; }

        /// <summary>12 entries, January first, for the comparison chart.</summary>
        public List<long> MonthlyPlaytimeSeconds { get; set; } = new List<long>();
    }

    /// <summary>One calendar month's playtime in both compared years, plus bar fractions (0-1)
    /// normalized against the higher of the two years' monthly totals - so a grouped-bar view can
    /// render both years' bars to the same scale without doing that math in XAML.</summary>
    public class YearComparisonMonthPoint
    {
        public int Month { get; set; }
        public long CurrentSeconds { get; set; }
        public long PreviousSeconds { get; set; }
        public double CurrentFraction { get; set; }
        public double PreviousFraction { get; set; }
    }

    /// <summary>Current selected year vs the immediately preceding year. Previous may be an
    /// all-zero period (never null) when there's no data for that year, so the UI can always bind
    /// without a null check.</summary>
    public class YearComparison
    {
        public YearComparisonPeriod Current { get; set; }
        public YearComparisonPeriod Previous { get; set; }

        /// <summary>Always 12 entries, January first.</summary>
        public List<YearComparisonMonthPoint> Months { get; set; } = new List<YearComparisonMonthPoint>();
    }
}
