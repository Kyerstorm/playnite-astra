using System;
using System.Collections.Generic;
using System.Linq;
using Astra.Data;
using Astra.Models;

namespace Astra.Services
{
    /// <summary>
    /// Computes a yearly recap on demand from Astra's own session table plus
    /// Playnite's library metadata. Nothing here is persisted — it's cheap
    /// enough to recompute each time the recap view opens.
    /// </summary>
    public class RecapAggregator
    {
        private readonly AstraDatabase database;
        private readonly IGameInfoProvider gameInfoProvider;

        public RecapAggregator(AstraDatabase database, IGameInfoProvider gameInfoProvider)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.gameInfoProvider = gameInfoProvider ?? throw new ArgumentNullException(nameof(gameInfoProvider));
        }

        public RecapData BuildRecap(int year)
        {
            var sessions = database.GetSessionsForYear(year);
            var games = gameInfoProvider.GetAllGames().ToDictionary(g => g.Id, g => g);
            var overrides = database.GetPlaytimeOverridesForYear(year);

            var recap = new RecapData
            {
                Year = year,
                TotalSessions = sessions.Count,
                ActiveDays = sessions.Select(s => s.StartedAt.Date).Distinct().Count()
            };

            recap.TopGames = sessions
                .GroupBy(s => s.GameId)
                .Select(g => new GameRecapEntry
                {
                    GameId = g.Key,
                    Name = games.TryGetValue(g.Key, out var info) ? info.Name : "Unknown game",
                    PlaytimeSeconds = overrides.TryGetValue(g.Key, out var overrideSeconds) ? overrideSeconds : g.Sum(s => s.DurationSeconds),
                    SessionCount = g.Count()
                })
                .OrderByDescending(e => e.PlaytimeSeconds)
                .ToList();

            // Recomputed from (possibly overridden) TopGames rather than the raw
            // session sum, so the total stays consistent with any manual edits.
            recap.TotalPlaytimeSeconds = recap.TopGames.Sum(g => g.PlaytimeSeconds);

            recap.NewGamesThisYear = games.Values
                .Where(g => g.Added.HasValue && g.Added.Value.Year == year)
                .Select(g => new GameRecapEntry
                {
                    GameId = g.Id,
                    Name = g.Name,
                    PlaytimeSeconds = recap.TopGames.FirstOrDefault(t => t.GameId == g.Id)?.PlaytimeSeconds ?? 0,
                    SessionCount = recap.TopGames.FirstOrDefault(t => t.GameId == g.Id)?.SessionCount ?? 0
                })
                .OrderByDescending(e => e.PlaytimeSeconds)
                .ToList();

            return recap;
        }
    }
}
