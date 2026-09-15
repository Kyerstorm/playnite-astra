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
        private const int TopCount = 25;

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

        public MostPlayedViewModel(Astra plugin, AstraSettings settings)
        {
            this.plugin = plugin;
            this.settings = settings;

            PreviousYearCommand = new RelayCommand(_ => Year--);
            NextYearCommand = new RelayCommand(_ => Year++, _ => Year < DateTime.Now.Year);
            EditPlaytimeCommand = new RelayCommand(p => EditPlaytime(p as GameRecapEntry));
            ResetPlaytimeCommand = new RelayCommand(p => ResetPlaytime(p as GameRecapEntry));

            year = settings.LastSelectedYear > 0 ? settings.LastSelectedYear : DateTime.Now.Year;
            Refresh();
        }

        private void Refresh()
        {
            var recap = plugin.RecapAggregator.BuildRecap(Year);
            Entries = recap.TopGames.Take(TopCount)
                .Select((e, i) => { e.Rank = i + 1; return e; })
                .ToList();
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
