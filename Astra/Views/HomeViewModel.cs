using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Astra.Models;

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
