using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Astra.Models;

namespace Astra.Views
{
    public class MostPlayedViewModel : ViewModelBase, IYearScoped
    {
        /// <summary>Selectable row caps for the display-count picker; int.MaxValue renders as "All".</summary>
        public static readonly List<int> DisplayCountOptions = new List<int> { 10, 25, 50, int.MaxValue };

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

        private int selectedDisplayCount;
        public int SelectedDisplayCount
        {
            get => selectedDisplayCount;
            set
            {
                if (SetValue(ref selectedDisplayCount, value))
                {
                    settings.MostPlayedDisplayCount = value;
                    Refresh();
                }
            }
        }

        private bool isGridView;
        public bool IsGridView
        {
            get => isGridView;
            set
            {
                if (SetValue(ref isGridView, value))
                {
                    settings.MostPlayedGridView = value;
                }
            }
        }

        private List<GameRecapEntry> entries;
        public List<GameRecapEntry> Entries
        {
            get => entries;
            private set => SetValue(ref entries, value);
        }

        private string statusMessage;
        public string StatusMessage
        {
            get => statusMessage;
            set => SetValue(ref statusMessage, value);
        }

        public ICommand PreviousYearCommand { get; }
        public ICommand NextYearCommand { get; }
        public ICommand EditPlaytimeCommand { get; }
        public ICommand ResetPlaytimeCommand { get; }
        public ICommand OpenGameDetailsCommand { get; }
        public ICommand ShowListViewCommand { get; }
        public ICommand ShowGridViewCommand { get; }

        public MostPlayedViewModel(Astra plugin, AstraSettings settings)
        {
            this.plugin = plugin;
            this.settings = settings;

            PreviousYearCommand = new RelayCommand(_ => Year--);
            NextYearCommand = new RelayCommand(_ => Year++, _ => Year < DateTime.Now.Year);
            EditPlaytimeCommand = new RelayCommand(p => EditPlaytime(p as GameRecapEntry));
            ResetPlaytimeCommand = new RelayCommand(p => ResetPlaytime(p as GameRecapEntry));
            OpenGameDetailsCommand = new RelayCommand(p => OpenGameDetails(p as GameRecapEntry));
            ShowListViewCommand = new RelayCommand(_ => IsGridView = false);
            ShowGridViewCommand = new RelayCommand(_ => IsGridView = true);

            year = settings.LastSelectedYear > 0 ? settings.LastSelectedYear : DateTime.Now.Year;
            selectedDisplayCount = DisplayCountOptions.Contains(settings.MostPlayedDisplayCount)
                ? settings.MostPlayedDisplayCount
                : 25;
            isGridView = settings.MostPlayedGridView;
            Refresh();
        }

        private void Refresh()
        {
            var recap = plugin.RecapAggregator.BuildRecap(Year);

            // Full (uncapped) prior-year ranking, purely to look up each game's rank for the
            // trend indicator - RecapAggregator is documented as cheap to recompute on demand.
            var previousYearRanks = plugin.RecapAggregator.BuildRecap(Year - 1).TopGames
                .Select((e, i) => (e.GameId, Rank: i + 1))
                .ToDictionary(x => x.GameId, x => x.Rank);

            var topPlaytime = recap.TopGames.Count > 0 ? recap.TopGames[0].PlaytimeSeconds : 0;

            Entries = recap.TopGames.Take(SelectedDisplayCount)
                .Select((e, i) =>
                {
                    e.Rank = i + 1;
                    e.PlaytimeShareOfTop = topPlaytime > 0 ? (double)e.PlaytimeSeconds / topPlaytime : 0;
                    e.PreviousYearRank = previousYearRanks.TryGetValue(e.GameId, out var prevRank) ? prevRank : (int?)null;
                    return e;
                })
                .ToList();
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

        private void EditPlaytime(GameRecapEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            var currentHours = entry.PlaytimeSeconds / 3600.0;
            var result = plugin.Api.Dialogs.SelectString(
                $"Enter corrected playtime in hours for \"{entry.Name}\" ({Year}):",
                "Edit Playtime",
                currentHours.ToString("0.##", CultureInfo.InvariantCulture));

            if (!result.Result)
            {
                return;
            }

            if (!double.TryParse(result.SelectedString, NumberStyles.Float, CultureInfo.InvariantCulture, out var hours) || hours < 0)
            {
                plugin.Api.Dialogs.ShowErrorMessage("Enter a valid, non-negative number of hours.", "Edit Playtime");
                return;
            }

            plugin.Database.SetPlaytimeOverride(entry.GameId, Year, (long)Math.Round(hours * 3600));
            Refresh();
            StatusMessage = $"Set {entry.Name}'s {Year} playtime to {hours:0.##}h.";
        }

        private void ResetPlaytime(GameRecapEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            plugin.Database.ClearPlaytimeOverride(entry.GameId, Year);
            Refresh();
            StatusMessage = $"Reset {entry.Name}'s {Year} playtime to the tracked value.";
        }
    }
}
