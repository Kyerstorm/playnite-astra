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
                .Select(g =>
                {
                    games.TryGetValue(g.Key, out var info);
                    var name = info?.Name ?? "Unknown game";
                    return new GameRecapEntry
                    {
                        GameId = g.Key,
                        Name = name,
                        PlaytimeSeconds = overrides.TryGetValue(g.Key, out var overrideSeconds) ? overrideSeconds : g.Sum(s => s.DurationSeconds),
                        SessionCount = g.Count(),
                        CoverImagePath = info?.CoverImagePath,
                        PlaceholderColorHex = CoverPlaceholder.ColorHexFor(name),
                        PlaceholderInitials = CoverPlaceholder.InitialsFor(name)
                    };
                })
                .OrderByDescending(e => e.PlaytimeSeconds)
                .ToList();

            // Recomputed from (possibly overridden) TopGames rather than the raw
            // session sum, so the total stays consistent with any manual edits.
            recap.TotalPlaytimeSeconds = recap.TopGames.Sum(g => g.PlaytimeSeconds);

            recap.NewGamesThisYear = games.Values
                .Where(g => g.Added.HasValue && g.Added.Value.Year == year)
                // Most-recently-added first - this is a "what's new" strip, not a leaderboard,
                // so it's ordered by Added date rather than playtime (unlike TopGames above).
                .OrderByDescending(g => g.Added)
                .Select(g => new GameRecapEntry
                {
                    GameId = g.Id,
                    Name = g.Name,
                    PlaytimeSeconds = recap.TopGames.FirstOrDefault(t => t.GameId == g.Id)?.PlaytimeSeconds ?? 0,
                    SessionCount = recap.TopGames.FirstOrDefault(t => t.GameId == g.Id)?.SessionCount ?? 0,
                    CoverImagePath = g.CoverImagePath,
                    PlaceholderColorHex = CoverPlaceholder.ColorHexFor(g.Name),
                    PlaceholderInitials = CoverPlaceholder.InitialsFor(g.Name)
                })
                .ToList();

            recap.GenreBreakdown = BuildCategoryBreakdown(recap.TopGames, games, gameInfo => gameInfo.Genres);
            recap.LibraryBreakdown = BuildCategoryBreakdown(recap.TopGames, games,
                gameInfo => string.IsNullOrEmpty(gameInfo.Library) ? new List<string>() : new List<string> { gameInfo.Library });

            return recap;
        }

        /// <summary>Lifetime equivalent of BuildRecap(year) - loops year-by-year through the
        /// existing bounded GetSessionsForYear/GetPlaytimeOverridesForYear queries (from the
        /// earliest tracked year through the current year) rather than adding a new full-table-scan
        /// database method, so per-year manual overrides are applied with the exact same precedence
        /// BuildRecap already uses. RecapData.Year is left at its default (0) - unused by Home's
        /// bindings, which read Recap.TotalPlaytimeSeconds/TopGames/etc. directly, never Recap.Year.
        /// RecapData.NewGamesThisYear is repurposed here to mean "games added in the trailing 30
        /// days" rather than "this calendar year" - same list shape, same GameRecapEntry template,
        /// only the filter differs (see HomeViewModel's section-header relabeling).</summary>
        public RecapData BuildAllTimeRecap()
        {
            var games = gameInfoProvider.GetAllGames().ToDictionary(g => g.Id, g => g);
            var earliestYear = database.GetEarliestSessionYear() ?? DateTime.Now.Year;

            var merged = new Dictionary<Guid, GameRecapEntry>();
            var totalSessions = 0;
            var activeDays = 0;

            for (var year = earliestYear; year <= DateTime.Now.Year; year++)
            {
                var sessions = database.GetSessionsForYear(year);
                if (sessions.Count == 0)
                {
                    continue;
                }

                var overrides = database.GetPlaytimeOverridesForYear(year);
                totalSessions += sessions.Count;
                activeDays += sessions.Select(s => s.StartedAt.Date).Distinct().Count();

                foreach (var g in sessions.GroupBy(s => s.GameId))
                {
                    var seconds = overrides.TryGetValue(g.Key, out var overrideSeconds)
                        ? overrideSeconds
                        : g.Sum(s => s.DurationSeconds);

                    if (!merged.TryGetValue(g.Key, out var entry))
                    {
                        games.TryGetValue(g.Key, out var info);
                        var name = info?.Name ?? "Unknown game";
                        entry = new GameRecapEntry
                        {
                            GameId = g.Key,
                            Name = name,
                            CoverImagePath = info?.CoverImagePath,
                            PlaceholderColorHex = CoverPlaceholder.ColorHexFor(name),
                            PlaceholderInitials = CoverPlaceholder.InitialsFor(name)
                        };
                        merged[g.Key] = entry;
                    }

                    entry.PlaytimeSeconds += seconds;
                    entry.SessionCount += g.Count();
                }
            }

            var recap = new RecapData
            {
                TotalSessions = totalSessions,
                ActiveDays = activeDays,
                TopGames = merged.Values.OrderByDescending(e => e.PlaytimeSeconds).ToList()
            };
            recap.TotalPlaytimeSeconds = recap.TopGames.Sum(g => g.PlaytimeSeconds);

            var recentCutoff = DateTime.Now.AddDays(-30);
            recap.NewGamesThisYear = games.Values
                .Where(g => g.Added.HasValue && g.Added.Value >= recentCutoff)
                .OrderByDescending(g => g.Added)
                .Select(g => new GameRecapEntry
                {
                    GameId = g.Id,
                    Name = g.Name,
                    PlaytimeSeconds = recap.TopGames.FirstOrDefault(t => t.GameId == g.Id)?.PlaytimeSeconds ?? 0,
                    SessionCount = recap.TopGames.FirstOrDefault(t => t.GameId == g.Id)?.SessionCount ?? 0,
                    CoverImagePath = g.CoverImagePath,
                    PlaceholderColorHex = CoverPlaceholder.ColorHexFor(g.Name),
                    PlaceholderInitials = CoverPlaceholder.InitialsFor(g.Name)
                })
                .ToList();

            recap.GenreBreakdown = BuildCategoryBreakdown(recap.TopGames, games, info => info.Genres);
            recap.LibraryBreakdown = BuildCategoryBreakdown(recap.TopGames, games,
                info => string.IsNullOrEmpty(info.Library) ? new List<string>() : new List<string> { info.Library });

            return recap;
        }

        /// <summary>Fans each game's (possibly-overridden) year playtime out to every one of its genres/
        /// platforms and sums by label. A game with multiple genres counts its FULL playtime under each
        /// one it belongs to - this is a deliberate, documented trade-off (see RecapAggregatorTests), not
        /// a bug: it answers "how much time went into RPGs" rather than trying to split one game's hours
        /// fractionally across genres, which would be arbitrary. Games with no genres/platforms recorded
        /// are silently excluded from the breakdown (never shown as "Uncategorized"). Capped at the top 6
        /// entries by playtime - this is a quick-glance widget, not an exhaustive report.</summary>
        private static List<CategoryBreakdownEntry> BuildCategoryBreakdown(
            List<GameRecapEntry> topGames,
            Dictionary<Guid, GameInfo> games,
            Func<GameInfo, List<string>> selectLabels)
        {
            var totals = new Dictionary<string, long>();

            foreach (var entry in topGames)
            {
                if (!games.TryGetValue(entry.GameId, out var info))
                {
                    continue;
                }

                foreach (var label in selectLabels(info) ?? new List<string>())
                {
                    totals[label] = totals.TryGetValue(label, out var existing) ? existing + entry.PlaytimeSeconds : entry.PlaytimeSeconds;
                }
            }

            if (totals.Count == 0)
            {
                return new List<CategoryBreakdownEntry>();
            }

            var sumOfAllEntries = totals.Values.Sum();
            var maxEntry = totals.Values.Max();

            return totals
                .OrderByDescending(kv => kv.Value)
                .Take(6)
                .Select(kv => new CategoryBreakdownEntry
                {
                    Label = kv.Key,
                    PlaytimeSeconds = kv.Value,
                    Percentage = sumOfAllEntries > 0 ? kv.Value * 100.0 / sumOfAllEntries : 0,
                    BarFraction = maxEntry > 0 ? kv.Value / (double)maxEntry : 0
                })
                .ToList();
        }
    }
}
