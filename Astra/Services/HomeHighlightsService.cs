using System.Collections.Generic;
using System.Linq;
using Astra.Models;

namespace Astra.Services
{
    /// <summary>Pure, DB/Playnite-free computations over already-built RecapData for Home's
    /// "highlights" strip. Mirrors LeaderboardSorter's shape: static class, plain model inputs,
    /// fully unit-testable without AstraDatabase or IPlayniteAPI.</summary>
    public static class HomeHighlightsService
    {
        /// <summary>The RETURNING game (had nonzero playtime in the previous year's TopGames) whose
        /// playtime grew the most this year, or null if no returning game improved. A brand-new game
        /// is deliberately excluded even though its full current-year total would technically be the
        /// "biggest jump" from zero - that's what Home's existing "New This Year" section already
        /// shows, and including new games here would make Most Improved redundant with it and would
        /// always crowd out genuine year-over-year growth stories.</summary>
        public static GameImprovementHighlight FindMostImproved(
            IEnumerable<GameRecapEntry> currentYearTopGames,
            IEnumerable<GameRecapEntry> previousYearTopGames)
        {
            var previousByGame = previousYearTopGames.ToDictionary(e => e.GameId, e => e.PlaytimeSeconds);

            GameImprovementHighlight best = null;
            foreach (var entry in currentYearTopGames)
            {
                if (!previousByGame.TryGetValue(entry.GameId, out var previousSeconds) || previousSeconds <= 0)
                {
                    continue; // not a returning game
                }

                var delta = entry.PlaytimeSeconds - previousSeconds;
                if (delta > 0 && (best == null || delta > best.DeltaSeconds))
                {
                    best = new GameImprovementHighlight
                    {
                        GameId = entry.GameId,
                        Name = entry.Name,
                        CoverImagePath = entry.CoverImagePath,
                        PlaceholderColorHex = entry.PlaceholderColorHex,
                        PlaceholderInitials = entry.PlaceholderInitials,
                        CurrentSeconds = entry.PlaytimeSeconds,
                        PreviousSeconds = previousSeconds,
                        DeltaSeconds = delta
                    };
                }
            }

            return best;
        }
    }
}
