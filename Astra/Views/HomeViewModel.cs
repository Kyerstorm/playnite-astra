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

        public string Greeting => string.IsNullOrWhiteSpace(settings.DisplayName)
            ? "Welcome back"
            : $"Welcome back, {settings.DisplayName}";

        public ICommand PreviousYearCommand { get; }
        public ICommand NextYearCommand { get; }

        public HomeViewModel(Astra plugin, AstraSettings settings)
        {
            this.plugin = plugin;
            this.settings = settings;

            PreviousYearCommand = new RelayCommand(_ => Year--);
            NextYearCommand = new RelayCommand(_ => Year++, _ => Year < DateTime.Now.Year);

            year = settings.LastSelectedYear > 0 ? settings.LastSelectedYear : DateTime.Now.Year;
            Refresh();
        }

        private void Refresh()
        {
            Recap = plugin.RecapAggregator.BuildRecap(Year);
            TopPlayed = Recap.TopGames.Take(10).ToList();
        }
    }
}
