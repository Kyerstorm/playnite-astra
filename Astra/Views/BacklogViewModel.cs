using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Astra.Models;
using Astra.Services;

namespace Astra.Views
{
    /// <summary>All-time (deliberately NOT IYearScoped) browsable list of never-played, non-hidden
    /// library games, with multi-select genre/platform chip filters. See CLAUDE.md's "Backlog page"
    /// section for the full set of scope decisions.</summary>
    public class BacklogViewModel : ViewModelBase
    {
        private readonly Astra plugin;
        private readonly Random random = new Random();

        private List<BacklogEntry> allEntries = new List<BacklogEntry>();

        private List<FilterOption> genreFilters = new List<FilterOption>();
        public List<FilterOption> GenreFilters
        {
            get => genreFilters;
            private set => SetValue(ref genreFilters, value);
        }

        private List<FilterOption> platformFilters = new List<FilterOption>();
        public List<FilterOption> PlatformFilters
        {
            get => platformFilters;
            private set => SetValue(ref platformFilters, value);
        }

        private List<BacklogEntry> entries = new List<BacklogEntry>();
        public List<BacklogEntry> Entries
        {
            get => entries;
            private set => SetValue(ref entries, value);
        }

        public ICommand OpenGameDetailsCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand PickRandomCommand { get; }

        public BacklogViewModel(Astra plugin, AstraSettings settings)
        {
            this.plugin = plugin;

            OpenGameDetailsCommand = new RelayCommand(p => OpenGameDetails(p as BacklogEntry));
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            PickRandomCommand = new RelayCommand(_ => PickRandom(), _ => Entries.Count > 0);

            Refresh();
        }

        public void Refresh()
        {
            var allGames = plugin.GameInfoProvider.GetAllGames();
            // "Played at any point, ever" - reuses GetPlayedGameIds(beforeExclusive) with the
            // furthest-future cutoff instead of adding a new all-time DB method (see plan Task 4).
            var everPlayed = plugin.Database.GetPlayedGameIds(DateTime.MaxValue);
            allEntries = BacklogService.BuildBacklog(allGames, everPlayed);

            GenreFilters = BacklogService.DistinctGenres(allEntries).Select(MakeFilter).ToList();
            PlatformFilters = BacklogService.DistinctPlatforms(allEntries).Select(MakeFilter).ToList();

            ApplyFilters();
        }

        private FilterOption MakeFilter(string label)
        {
            var option = new FilterOption(label);
            option.PropertyChanged += (_, __) => ApplyFilters();
            return option;
        }

        private void ApplyFilters()
        {
            var selectedGenres = GenreFilters.Where(f => f.IsSelected).Select(f => f.Label).ToList();
            var selectedPlatforms = PlatformFilters.Where(f => f.IsSelected).Select(f => f.Label).ToList();
            Entries = BacklogService.ApplyFilters(allEntries, selectedGenres, selectedPlatforms);
        }

        private void ClearFilters()
        {
            // Each IsSelected = false re-triggers ApplyFilters via the PropertyChanged hook in MakeFilter.
            foreach (var filter in GenreFilters.Concat(PlatformFilters))
            {
                filter.IsSelected = false;
            }
        }

        /// <summary>Jumps to a random game from the currently filtered Entries - reuses OpenGameDetails
        /// so it's the exact same navigation as clicking a card. Uninfluenced by sort order (which is
        /// always alphabetical) since it draws straight from Entries's current index range.</summary>
        private void PickRandom()
        {
            if (Entries.Count == 0)
            {
                return;
            }

            OpenGameDetails(Entries[random.Next(Entries.Count)]);
        }

        /// <summary>Jumps to the game's own details page in Playnite's library view.</summary>
        private void OpenGameDetails(BacklogEntry entry)
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
