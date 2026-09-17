using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Astra.Models;
using Astra.Services;

namespace Astra.Views
{
    /// <summary>
    /// Drives the dedicated Trends page. Each of the four tabs (Day/Week/Month/Year) shows exactly
    /// ONE period of its own length - a single day, a single Monday-start week, a single calendar
    /// month, or a single calendar year - picked via the period dropdown, rather than a rolling
    /// multi-period window. All numbers come from TrendAggregationService/PlaytimeInsightsService;
    /// this view-model only resolves "which single period" into concrete dates and holds UI state.
    /// </summary>
    public class TrendsViewModel : ViewModelBase
    {
        private readonly Astra plugin;

        private TrendGranularity granularity;
        public TrendGranularity Granularity
        {
            get => granularity;
            set
            {
                if (SetValue(ref granularity, value))
                {
                    NotifyPropertyChanged(nameof(IsDaySelected));
                    NotifyPropertyChanged(nameof(IsWeekSelected));
                    NotifyPropertyChanged(nameof(IsMonthSelected));
                    NotifyPropertyChanged(nameof(IsYearSelected));
                    NotifyPropertyChanged(nameof(IsYearComparisonVisible));

                    AvailablePeriods = BuildAvailablePeriods(value);
                    // Selecting the most recent period triggers Refresh() via SelectedPeriod's setter -
                    // every granularity's period list is distinct, so this always fires.
                    SelectedPeriod = AvailablePeriods.Count > 0 ? AvailablePeriods[0] : null;
                }
            }
        }

        public bool IsDaySelected => Granularity == TrendGranularity.Day;
        public bool IsWeekSelected => Granularity == TrendGranularity.Week;
        public bool IsMonthSelected => Granularity == TrendGranularity.Month;
        public bool IsYearSelected => Granularity == TrendGranularity.Year;

        /// <summary>Only Year mode compares against a full preceding year - a single day/week/month
        /// doesn't have an obvious "same period last year" counterpart worth building yet.</summary>
        public bool IsYearComparisonVisible => Granularity == TrendGranularity.Year;

        private List<TrendPeriodOption> availablePeriods = new List<TrendPeriodOption>();
        public List<TrendPeriodOption> AvailablePeriods
        {
            get => availablePeriods;
            private set => SetValue(ref availablePeriods, value);
        }

        private TrendPeriodOption selectedPeriod;
        public TrendPeriodOption SelectedPeriod
        {
            get => selectedPeriod;
            set
            {
                if (SetValue(ref selectedPeriod, value) && value != null)
                {
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

        /// <summary>Always one full calendar year of daily buckets for the year the selected period
        /// falls in - a calendar heatmap for a single day or week wouldn't be meaningful, so it stays
        /// pinned to that period's year for context (spec sections 10, 20).</summary>
        public TrendResult HeatmapTrend
        {
            get => heatmapTrend;
            private set => SetValue(ref heatmapTrend, value);
        }

        private YearComparison yearComparison;
        public YearComparison YearComparison
        {
            get => yearComparison;
            private set => SetValue(ref yearComparison, value);
        }

        private List<string> gamingYearSummary;
        public List<string> GamingYearSummary
        {
            get => gamingYearSummary;
            private set => SetValue(ref gamingYearSummary, value);
        }

        public ICommand SelectDayCommand { get; }
        public ICommand SelectWeekCommand { get; }
        public ICommand SelectMonthCommand { get; }
        public ICommand SelectYearCommand { get; }

        public TrendsViewModel(Astra plugin, AstraSettings settings)
        {
            this.plugin = plugin;

            SelectDayCommand = new RelayCommand(_ => Granularity = TrendGranularity.Day);
            SelectWeekCommand = new RelayCommand(_ => Granularity = TrendGranularity.Week);
            SelectMonthCommand = new RelayCommand(_ => Granularity = TrendGranularity.Month);
            SelectYearCommand = new RelayCommand(_ => Granularity = TrendGranularity.Year);

            granularity = TrendGranularity.Month;
            availablePeriods = BuildAvailablePeriods(granularity);
            selectedPeriod = availablePeriods.Count > 0 ? availablePeriods[0] : null;

            Refresh();
        }

        /// <summary>Opens Trends on the Year tab with the given year selected - used by Home's
        /// "View Trends ->" link (AstraShellViewModel wires HomeViewModel.ViewTrendsRequested here).
        /// Year is the only tab that can reach an arbitrary past year regardless of how long ago it
        /// was, since Month/Week/Day only ever list the most recent handful of periods.</summary>
        public void ShowYear(int year)
        {
            Granularity = TrendGranularity.Year;

            var match = AvailablePeriods.FirstOrDefault(p => p.PeriodStart.Year == year);
            if (match == null)
            {
                match = new TrendPeriodOption
                {
                    Label = year.ToString(CultureInfo.InvariantCulture),
                    PeriodStart = new DateTime(year, 1, 1),
                    PeriodEnd = new DateTime(year + 1, 1, 1)
                };
            }

            SelectedPeriod = match;
        }

        private void Refresh()
        {
            if (SelectedPeriod == null)
            {
                return;
            }

            var start = SelectedPeriod.PeriodStart;
            var end = SelectedPeriod.PeriodEnd;

            Trend = plugin.TrendAggregationService.BuildTrend(ChartBucketGranularity(Granularity), start, end);

            var (previousStart, previousEnd) = PreviousPeriodRange(Granularity, start, end);
            var previousTrend = plugin.TrendAggregationService.BuildTrend(ChartBucketGranularity(Granularity), previousStart, previousEnd);
            PreviousPeriodDeltaSeconds = Trend.TotalPlaytimeSeconds - previousTrend.TotalPlaytimeSeconds;

            Analytics = plugin.PlaytimeInsightsService.Analyze(start, end);
            HeatmapTrend = plugin.TrendAggregationService.BuildTrend(TrendGranularity.Day, new DateTime(start.Year, 1, 1), new DateTime(start.Year + 1, 1, 1));
            YearComparison = plugin.PlaytimeInsightsService.BuildYearComparison(start.Year);
            GamingYearSummary = GamingYearSummaryBuilder.Build(Analytics, Analytics?.Concentration);
        }

        /// <summary>The main chart's internal bucket size for the selected tab - one level finer than
        /// the tab itself, since each tab now shows a single period rather than a trend across several:
        /// Day -> hourly bars within that day, Week/Month -> daily bars within that week/month,
        /// Year -> monthly bars within that year.</summary>
        private static TrendGranularity ChartBucketGranularity(TrendGranularity tab)
        {
            switch (tab)
            {
                case TrendGranularity.Day:
                    return TrendGranularity.Hour;
                case TrendGranularity.Week:
                    return TrendGranularity.Day;
                case TrendGranularity.Month:
                    return TrendGranularity.Day;
                case TrendGranularity.Year:
                    return TrendGranularity.Month;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tab));
            }
        }

        private static (DateTime start, DateTime end) PreviousPeriodRange(TrendGranularity granularity, DateTime start, DateTime end)
        {
            switch (granularity)
            {
                case TrendGranularity.Day:
                    return (start.AddDays(-1), start);
                case TrendGranularity.Week:
                    return (start.AddDays(-7), start);
                case TrendGranularity.Month:
                    return (start.AddMonths(-1), start);
                case TrendGranularity.Year:
                    return (start.AddYears(-1), start);
                default:
                    throw new ArgumentOutOfRangeException(nameof(granularity));
            }
        }

        /// <summary>Builds the period dropdown's contents for a given tab - see TrendPeriodOption for
        /// what each entry represents. Counts/labels match the exact spec given for this redesign:
        /// Day = last 7 individual days, Week = current + last 3 individual weeks, Month = current +
        /// previous 11 individual months, Year = every year with recorded data, most recent first.</summary>
        private List<TrendPeriodOption> BuildAvailablePeriods(TrendGranularity forGranularity)
        {
            var today = DateTime.Now.Date;
            var options = new List<TrendPeriodOption>();

            switch (forGranularity)
            {
                case TrendGranularity.Day:
                    for (var i = 0; i < 7; i++)
                    {
                        var date = today.AddDays(-i);
                        options.Add(new TrendPeriodOption
                        {
                            Label = date.ToString("d/M/yyyy", CultureInfo.InvariantCulture),
                            PeriodStart = date,
                            PeriodEnd = date.AddDays(1)
                        });
                    }
                    break;

                case TrendGranularity.Week:
                    var thisWeekStart = TrendAggregationService.StartOfWeek(today);
                    for (var i = 0; i < 4; i++)
                    {
                        var weekStart = thisWeekStart.AddDays(-7 * i);
                        var weekEnd = weekStart.AddDays(7);
                        options.Add(new TrendPeriodOption
                        {
                            Label = $"{weekStart.ToString("d MMM", CultureInfo.InvariantCulture)} - {weekEnd.AddDays(-1).ToString("d MMM yyyy", CultureInfo.InvariantCulture)}",
                            PeriodStart = weekStart,
                            PeriodEnd = weekEnd
                        });
                    }
                    break;

                case TrendGranularity.Month:
                    var thisMonthStart = new DateTime(today.Year, today.Month, 1);
                    for (var i = 0; i < 12; i++)
                    {
                        var monthStart = thisMonthStart.AddMonths(-i);
                        options.Add(new TrendPeriodOption
                        {
                            Label = monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                            PeriodStart = monthStart,
                            PeriodEnd = monthStart.AddMonths(1)
                        });
                    }
                    break;

                case TrendGranularity.Year:
                    var earliestYear = plugin.Database.GetEarliestSessionYear() ?? today.Year;
                    for (var y = today.Year; y >= earliestYear; y--)
                    {
                        options.Add(new TrendPeriodOption
                        {
                            Label = y.ToString(CultureInfo.InvariantCulture),
                            PeriodStart = new DateTime(y, 1, 1),
                            PeriodEnd = new DateTime(y + 1, 1, 1)
                        });
                    }
                    break;
            }

            return options;
        }
    }
}
