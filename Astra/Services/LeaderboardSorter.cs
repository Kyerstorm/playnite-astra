using System.Collections.Generic;
using System.Linq;
using Astra.Models;

namespace Astra.Services
{
    public enum LeaderboardSortMode
    {
        Playtime,
        Sessions,
        AverageSessionLength
    }

    /// <summary>
    /// Pure re-sort of an already-computed set of GameRecapEntry rows (e.g.
    /// RecapData.TopGames) by a different metric. Does not touch the
    /// underlying playtime-descending order RecapAggregator produces —
    /// callers keep that around separately since NewGamesThisYear and the
    /// manual-override total depend on it.
    /// </summary>
    public static class LeaderboardSorter
    {
        public static List<GameRecapEntry> Sort(IEnumerable<GameRecapEntry> entries, LeaderboardSortMode mode)
        {
            switch (mode)
            {
                case LeaderboardSortMode.Sessions:
                    return entries.OrderByDescending(e => e.SessionCount).ToList();
                case LeaderboardSortMode.AverageSessionLength:
                    return entries.OrderByDescending(e => e.AvgSessionSeconds).ToList();
                default:
                    return entries.OrderByDescending(e => e.PlaytimeSeconds).ToList();
            }
        }
    }
}
