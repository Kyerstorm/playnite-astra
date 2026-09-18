using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Astra.Models;
using Astra.Services;

namespace Astra.Views
{
    public class HomeViewModel : ViewModelBase, IYearScoped
    {
        private readonly Astra plugin;
        private readonly AstraSettings settings;

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

        private RecapData recap;
        public RecapData Recap
        {
            get => recap;
            private set => SetValue(ref recap, value);
        }

        private List<GameRecapEntry> topPlayed;
        public List<GameRecapEntry> TopPlayed
        {
            get => topPlayed;
            private set => SetValue(ref topPlayed, value);
        }

        private TrendResult homeTrend;

        /// <summary>Monthly playtime for the selected year, feeding the compact "Playtime Trend"
        /// section - same TrendAggregationService call the Trends page's Month/&lt;year&gt; view uses,
        /// so the two can never disagree (see CLAUDE.md sync requirement).</summary>
        public TrendResult HomeTrend
        {
            get => homeTrend;
            private set => SetValue(ref homeTrend, value);
        }

        public string Greeting => string.IsNullOrWhiteSpace(settings.DisplayName)
            ? "Welcome back"
            : $"Welcome back, {settings.DisplayName}";

        private long thisWeekSeconds;

        /// <summary>Trailing 7-calendar-day window ending today - deliberately NOT year-scoped and
        /// NOT the Monday-start "week" convention Trends uses. It's a "right now" fact, shown the
        /// same regardless of which Year the page is currently browsing.</summary>
        public long ThisWeekSeconds
        {
            get => thisWeekSeconds;
            private set => SetValue(ref thisWeekSeconds, value);
        }

        private List<CategoryBreakdownEntry> genreBreakdown;
        public List<CategoryBreakdownEntry> GenreBreakdown
        {
            get => genreBreakdown;
            private set => SetValue(ref genreBreakdown, value);
        }

        private List<CategoryBreakdownEntry> platformBreakdown;
        public List<CategoryBreakdownEntry> PlatformBreakdown
        {
            get => platformBreakdown;
            private set => SetValue(ref platformBreakdown, value);
        }

        private BacklogBurndownStats backlog;
        public BacklogBurndownStats Backlog
        {
            get => backlog;
            private set => SetValue(ref backlog, value);
        }

        private string backlogDeltaText;

        /// <summary>Precomputed in C# rather than via a WPF StringFormat sign-section (a
        /// "+0.0;-0.0;+0.0"-style custom numeric format is fragile inside a XAML attribute) -
        /// same "format text in the ViewModel/service, not in XAML" convention as
        /// GamingYearSummaryBuilder.</summary>
        public string BacklogDeltaText
        {
            get => backlogDeltaText;
            private set => SetValue(ref backlogDeltaText, value);
        }

        private SessionCadenceHighlight sessionCadence;
        public SessionCadenceHighlight SessionCadence
        {
            get => sessionCadence;
            private set => SetValue(ref sessionCadence, value);
        }

        public ICommand PreviousYearCommand { get; }
        public ICommand NextYearCommand { get; }
        public ICommand OpenGameDetailsCommand { get; }
        public ICommand ViewTrendsCommand { get; }

        /// <summary>Raised by ViewTrendsCommand, carrying the currently-selected year. AstraShellViewModel
        /// wires this to navigate to the Trends page scoped to Month/&lt;that year&gt; - see section 14
        /// of the Trends spec ("View Trends" preserves Home's year context).</summary>
        public event Action<int> ViewTrendsRequested;

        public HomeViewModel(Astra plugin, AstraSettings settings)
        {
            this.plugin = plugin;
            this.settings = settings;

            PreviousYearCommand = new RelayCommand(_ => Year--);
            NextYearCommand = new RelayCommand(_ => Year++, _ => Year < DateTime.Now.Year);
            OpenGameDetailsCommand = new RelayCommand(p => OpenGameDetails(p as GameRecapEntry));
            ViewTrendsCommand = new RelayCommand(_ => ViewTrendsRequested?.Invoke(Year));

            year = settings.LastSelectedYear > 0 ? settings.LastSelectedYear : DateTime.Now.Year;
            Refresh();
        }

        private void Refresh()
        {
            Recap = plugin.RecapAggregator.BuildRecap(Year);
            TopPlayed = Recap.TopGames.Take(10).ToList();
            HomeTrend = plugin.TrendAggregationService.BuildMonthlyTrendForYear(Year);

            var weekStart = DateTime.Now.Date.AddDays(-6);
            var weekEnd = DateTime.Now.Date.AddDays(1);
            ThisWeekSeconds = plugin.TrendAggregationService.BuildTrend(TrendGranularity.Day, weekStart, weekEnd).TotalPlaytimeSeconds;

            GenreBreakdown = Recap.GenreBreakdown;
            PlatformBreakdown = Recap.PlatformBreakdown;

            var allGames = plugin.GameInfoProvider.GetAllGames().ToList();

            // Backlog cutoffs match RecapAggregator/GetSessionsForYear's own year-boundary
            // convention (exclusive end at Jan 1 of the following year) so this stays consistent
            // with whichever Year Home is currently browsing - unlike ThisWeekSeconds above,
            // which is deliberately NOT year-scoped.
            var currentCutoff = new DateTime(Year + 1, 1, 1);
            var previousCutoff = new DateTime(Year, 1, 1);
            var playedAsOfCurrent = plugin.Database.GetPlayedGameIds(currentCutoff);
            var playedAsOfPrevious = plugin.Database.GetPlayedGameIds(previousCutoff);
            Backlog = BacklogBurndownService.Compute(allGames, playedAsOfCurrent, playedAsOfPrevious, currentCutoff, previousCutoff);
            BacklogDeltaText = Backlog == null
                ? null
                : $"Backlog {(Backlog.DeltaPercentagePoints >= 0 ? "+" : "")}{Backlog.DeltaPercentagePoints:0.0}pt vs last year";

            var yearAnalytics = plugin.PlaytimeInsightsService.Analyze(new DateTime(Year, 1, 1), new DateTime(Year + 1, 1, 1));
            SessionCadence = SessionCadenceService.Describe(yearAnalytics.SessionLengths);
        }

        /// <summary>Jumps to the game's own details page in Playnite's library view.</summary>
        private void OpenGameDetails(GameRecapEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            plugin.Api.MainView.SwitchToLibraryView();
            plugin.Api.MainView.SelectGame(entry.GameId);
        }
    }
}
