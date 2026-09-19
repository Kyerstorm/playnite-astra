using System;
using System.Collections.Generic;
using System.Linq;
using Astra.Models;

namespace Astra.Services
{
    /// <summary>Pure, DB/Playnite-free computation of the year-over-year backlog trend.
    /// Mirrors HomeHighlightsService's shape: static class, plain model inputs, fully
    /// unit-testable. A game with no Added date is treated as always-owned (can't know when
    /// it was acquired, so it's assumed pre-owned in every snapshot) - the same kind of
    /// documented trade-off RecapAggregator makes for NewGamesThisYear (only games WITH an
    /// Added date can be "new"; here, games WITHOUT one are the ones assumed pre-existing).</summary>
    public static class BacklogBurndownService
    {
        public static BacklogBurndownStats Compute(
            IEnumerable<GameInfo> allGames,
            HashSet<Guid> playedGameIdsAsOfCurrentCutoff,
            HashSet<Guid> playedGameIdsAsOfPreviousCutoff,
            DateTime currentCutoff,
            DateTime previousCutoff)
        {
            var games = allGames.ToList();

            var (currentOwned, currentUnplayed, currentPercent) =
                ComputeSnapshot(games, playedGameIdsAsOfCurrentCutoff, currentCutoff);
            var (_, _, previousPercent) =
                ComputeSnapshot(games, playedGameIdsAsOfPreviousCutoff, previousCutoff);

            return new BacklogBurndownStats
            {
                OwnedGameCount = currentOwned,
                UnplayedGameCount = currentUnplayed,
                BacklogPercent = currentPercent,
                PreviousYearBacklogPercent = previousPercent,
                DeltaPercentagePoints = currentPercent - previousPercent
            };
        }

        /// <summary>Lifetime backlog snapshot with no year-over-year comparison - there is no
        /// "previous year" in All-Time mode. Reuses the same ComputeSnapshot private helper as
        /// Compute, with DateTime.MaxValue as the cutoff so every game with any Added date (or none)
        /// counts as owned.</summary>
        public static BacklogBurndownStats ComputeAllTime(IEnumerable<GameInfo> allGames, HashSet<Guid> everPlayedGameIds)
        {
            var (owned, unplayed, percent) = ComputeSnapshot(allGames.ToList(), everPlayedGameIds, DateTime.MaxValue);
            return new BacklogBurndownStats
            {
                OwnedGameCount = owned,
                UnplayedGameCount = unplayed,
                BacklogPercent = percent,
                PreviousYearBacklogPercent = percent,
                DeltaPercentagePoints = 0
            };
        }

        private static (int owned, int unplayed, double percent) ComputeSnapshot(
            List<GameInfo> games, HashSet<Guid> playedGameIds, DateTime cutoff)
        {
            var owned = games.Where(g => !g.Added.HasValue || g.Added.Value < cutoff).ToList();
            var unplayed = owned.Count(g => !playedGameIds.Contains(g.Id));
            var percent = owned.Count > 0 ? unplayed * 100.0 / owned.Count : 0;
            return (owned.Count, unplayed, percent);
        }
    }
}
