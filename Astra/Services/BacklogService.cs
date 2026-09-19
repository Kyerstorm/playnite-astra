using System;
using System.Collections.Generic;
using System.Linq;
using Astra.Models;

namespace Astra.Services
{
    /// <summary>Pure, DB/Playnite-free computation of the Backlog page's game list. Mirrors
    /// BacklogBurndownService/LeaderboardSorter's shape: static class, plain model inputs, fully
    /// unit-testable. Deliberately all-time (not year-scoped) - the caller passes
    /// AstraDatabase.GetPlayedGameIds(DateTime.MaxValue) for "played at any point, ever" rather
    /// than this service or a new DB method taking a year/cutoff.</summary>
    public static class BacklogService
    {
        /// <summary>allGames -> alphabetical (A-Z, case-insensitive) list of non-hidden games with no
        /// entry in everPlayedGameIds.</summary>
        public static List<BacklogEntry> BuildBacklog(IEnumerable<GameInfo> allGames, HashSet<Guid> everPlayedGameIds)
        {
            return allGames
                .Where(g => !g.IsHidden && !everPlayedGameIds.Contains(g.Id))
                .Select(g => new BacklogEntry
                {
                    GameId = g.Id,
                    Name = g.Name,
                    Added = g.Added,
                    CoverImagePath = g.CoverImagePath,
                    PlaceholderColorHex = CoverPlaceholder.ColorHexFor(g.Name),
                    PlaceholderInitials = CoverPlaceholder.InitialsFor(g.Name),
                    Genres = g.Genres,
                    Platforms = g.Platforms
                })
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>AND-across-categories, OR-within-category: a game must match at least one selected
        /// genre (if any genre is selected) AND at least one selected platform (if any platform is
        /// selected). No filters selected in a category means that category imposes no constraint.</summary>
        public static List<BacklogEntry> ApplyFilters(
            IEnumerable<BacklogEntry> entries,
            IReadOnlyCollection<string> selectedGenres,
            IReadOnlyCollection<string> selectedPlatforms)
        {
            return entries.Where(e =>
                (selectedGenres.Count == 0 || e.Genres.Any(selectedGenres.Contains)) &&
                (selectedPlatforms.Count == 0 || e.Platforms.Any(selectedPlatforms.Contains))
            ).ToList();
        }

        /// <summary>Distinct genre names present in the (already-unplayed) backlog itself, not the
        /// whole library - so no filter chip can ever produce a zero-result list on first click.</summary>
        public static List<string> DistinctGenres(IEnumerable<BacklogEntry> entries)
        {
            return entries.SelectMany(e => e.Genres)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<string> DistinctPlatforms(IEnumerable<BacklogEntry> entries)
        {
            return entries.SelectMany(e => e.Platforms)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
