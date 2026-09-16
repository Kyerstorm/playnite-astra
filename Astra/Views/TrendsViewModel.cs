using System;
using System.Collections.Generic;
using System.Windows.Input;
using Astra.Models;

namespace Astra.Views
{
    /// <summary>
    /// Drives the dedicated Trends page: Day/Week/Month/Year granularity, a range preset per
    /// granularity, and (Month only) a year selector shared with Home/Most Played/Recap via
    /// AstraSettings.LastSelectedYear. All numbers come from TrendAggregationService - this
    /// view-model only resolves "which range" into concrete dates and holds UI state.
    /// </summary>
    public class TrendsViewModel : ViewModelBase, IYearScoped
    {
        private readonly Astra plugin;
        private readonly AstraSettings settings;

        public static readonly Dictionary<TrendGranularity, List<string>> RangeOptionsByGranularity = new Dictionary<TrendGranularity, List<string>>
        {
            [TrendGranularity.Day] = new List<string> { "Last 7 days", "Last 14 days", "Last 30 days" },
            [TrendGranularity.Week] = new List<string> { "Last 8 weeks", "Last 12 weeks", "Last 26 weeks" },
            [TrendGranularity.Month] = new List<string> { "Current year", "Previous year", "Last 2 years" },
            [TrendGranularity.Year] = new List<string> { "All years", "Last 5 years", "Last 10 years" }
        };

        private static readonly Dictionary<TrendGranularity, string> DefaultRangeOption = new Dictionary<TrendGranularity, string>
        {
            [TrendGranularity.Day] = "Last 30 days",
            [TrendGranularity.Week] = "Last 12 weeks",
            [TrendGranularity.Month] = "Current year",
            [TrendGranularity.Year] = "All years"
        };

        private TrendGranularity granularity;
        public TrendGranularity Granularity
        {
            get => granularity;
            set
            {
                if (SetValue(ref granularity, value))
                {
                    NotifyPropertyChanged(nameof(AvailableRangeOptions));
                    NotifyPropertyChanged(nameof(IsYearNavVisible));
                    NotifyPropertyChanged(nameof(IsDaySelected));
                    NotifyPropertyChanged(nameof(IsWeekSelected));
                    NotifyPropertyChanged(nameof(IsMonthSelected));
                    NotifyPropertyChanged(nameof(IsYearSelected));
                    // Assigning SelectedRangeOption triggers Refresh() via its own setter - every
                    // granularity's default label is distinct, so this always fires even when the
                    // previous granularity happened to leave the same string selected.
                    SelectedRangeOption = DefaultRangeOption[value];
                }
            }
        }

        public List<string> AvailableRangeOptions => RangeOptionsByGranularity[Granularity];

        private string selectedRangeOption;
        public string SelectedRangeOption
        {
            get => selectedRangeOption;
            set
            {
                if (SetValue(ref selectedRangeOption, value))
                {
                    Refresh();
                }
            }
        }

        /// <summary>Year nav only applies to Month mode: Day/Week ranges are always relative to "now"
        /// ("last 30 days"), and Year mode's own presets ("All years"/"Last 5/10 years") are relative
        /// windows too - see spec section 6, which only gives a Month example.</summary>
        public bool IsYearNavVisible => Granularity == TrendGranularity.Month;

        public bool IsDaySelected => Granularity == TrendGranularity.Day;
        public bool IsWeekSelected => Granularity == TrendGranularity.Week;
        public bool IsMonthSelected => Granularity == TrendGranularity.Month;
        public bool IsYearSelected => Granularity == TrendGranularity.Year;

        private int year;
        public int Year
        {
            get => year;
            set
            {
                if (SetValue(ref year, value))
                {
                    settings.LastSelectedYear = value;
                    Refresh();
                }
            }
        }

        private TrendResult trend;
        public TrendResult Trend
        {
            get => trend;
            private set => SetValue(ref trend, value);
        }

        private long previousPeriodDeltaSeconds;
        public long PreviousPeriodDeltaSeconds
        {
            get => previousPeriodDeltaSeconds;
            private set => SetValue(ref previousPeriodDeltaSeconds, value);
        }

        private TrendAnalyticsResult analytics;
        public TrendAnalyticsResult Analytics
        {
            get => analytics;
            private set => SetValue(ref analytics, value);
        }

        private TrendResult heatmapTrend;

        /// <summary>Always one full calendar year of daily buckets for the currently-scoped Year
        /// (settings.LastSelectedYear), independent of the main chart's granularity - a calendar
        /// heatmap for "last 7 days" or a 10-year "All years" range wouldn't be meaningful, so it
        /// stays pinned to a single year the way Home's mini-chart does (spec sections 10, 20).</summary>
        public TrendResult HeatmapTrend
        {
            get => heatmapTrend;
            private set => SetValue(ref heatmapTrend, value);
        }

        public ICommand PreviousYearCommand { get; }
        public ICommand NextYearCommand { get; }
        public ICommand SelectDayCommand { get; }
        public ICommand SelectWeekCommand { get; }
        public ICommand SelectMonthCommand { get; }
        public ICommand SelectYearCommand { get; }

        public TrendsViewModel(Astra plugin, AstraSettings settings)
        {
            this.plugin = plugin;
            this.settings = settings;

            granularity = TrendGranularity.Month;
            year = settings.LastSelectedYear > 0 ? settings.LastSelectedYear : DateTime.Now.Year;
            selectedRangeOption = DefaultRangeOption[granularity];

            PreviousYearCommand = new RelayCommand(_ => Year--);
            NextYearCommand = new RelayCommand(_ => Year++, _ => Year < DateTime.Now.Year);
            SelectDayCommand = new RelayCommand(_ => Granularity = TrendGranularity.Day);
            SelectWeekCommand = new RelayCommand(_ => Granularity = TrendGranularity.Week);
            SelectMonthCommand = new RelayCommand(_ => Granularity = TrendGranularity.Month);
            SelectYearCommand = new RelayCommand(_ => Granularity = TrendGranularity.Year);

            Refresh();
        }

        /// <summary>Opens Trends already scoped to Month/&lt;targetYear&gt;, preserving Home's year
        /// context - see AstraShellViewModel's wiring of HomeViewModel.ViewTrendsRequested.</summary>
        public void ShowMonthlyTrendForYear(int targetYear)
        {
            var wasAlreadyMonth = granularity == TrendGranularity.Month;
            granularity = TrendGranularity.Month;
            selectedRangeOption = DefaultRangeOption[TrendGranularity.Month];
            NotifyPropertyChanged(nameof(Granularity));
            NotifyPropertyChanged(nameof(AvailableRangeOptions));
            NotifyPropertyChanged(nameof(SelectedRangeOption));
            NotifyPropertyChanged(nameof(IsYearNavVisible));
            NotifyPropertyChanged(nameof(IsDaySelected));
            NotifyPropertyChanged(nameof(IsWeekSelected));
            NotifyPropertyChanged(nameof(IsMonthSelected));
            NotifyPropertyChanged(nameof(IsYearSelected));

            if (wasAlreadyMonth && year == targetYear)
            {
                Refresh();
            }
            else
            {
                Year = targetYear; // setter refreshes
            }
        }

        private void Refresh()
        {
            var (start, end) = ComputeRange();
            Trend = plugin.TrendAggregationService.BuildTrend(Granularity, start, end);

            var previousStart = start - (end - start);
            var previousTrend = plugin.TrendAggregationService.BuildTrend(Granularity, previousStart, start);
            PreviousPeriodDeltaSeconds = Trend.TotalPlaytimeSeconds - previousTrend.TotalPlaytimeSeconds;

            Analytics = plugin.PlaytimeInsightsService.Analyze(start, end);
            HeatmapTrend = plugin.TrendAggregationService.BuildTrend(TrendGranularity.Day, new DateTime(Year, 1, 1), new DateTime(Year + 1, 1, 1));
        }

        private (DateTime start, DateTime end) ComputeRange()
        {
            var today = DateTime.Now.Date;

            switch (Granularity)
            {
                case TrendGranularity.Day:
                    var days = SelectedRangeOption == "Last 7 days" ? 7 : SelectedRangeOption == "Last 14 days" ? 14 : 30;
                    return (today.AddDays(-(days - 1)), today.AddDays(1));

                case TrendGranularity.Week:
                    var weeks = SelectedRangeOption == "Last 8 weeks" ? 8 : SelectedRangeOption == "Last 26 weeks" ? 26 : 12;
                    return (today.AddDays(-7 * weeks), today.AddDays(1));

                case TrendGranularity.Month:
                    if (SelectedRangeOption == "Previous year")
                    {
                        return (new DateTime(Year - 1, 1, 1), new DateTime(Year, 1, 1));
                    }
                    if (SelectedRangeOption == "Last 2 years")
                    {
                        return (new DateTime(Year - 1, 1, 1), new DateTime(Year + 1, 1, 1));
                    }
                    return (new DateTime(Year, 1, 1), new DateTime(Year + 1, 1, 1));

                case TrendGranularity.Year:
                    if (SelectedRangeOption == "Last 5 years")
                    {
                        return (new DateTime(today.Year - 4, 1, 1), new DateTime(today.Year + 1, 1, 1));
                    }
                    if (SelectedRangeOption == "Last 10 years")
                    {
                        return (new DateTime(today.Year - 9, 1, 1), new DateTime(today.Year + 1, 1, 1));
                    }
                    var earliestYear = plugin.Database.GetEarliestSessionYear() ?? today.Year;
                    return (new DateTime(earliestYear, 1, 1), new DateTime(today.Year + 1, 1, 1));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
