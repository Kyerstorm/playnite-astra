using System;
using System.Collections.Generic;

namespace Astra.Models
{
    /// <summary>Which way an entry's rank moved versus the prior year, for the Most Played trend indicator.</summary>
    public enum RankTrend
    {
        /// <summary>Not present in the prior year's ranked list at all.</summary>
        New,
        Up,
        Down,
        Same
    }

    public class GameRecapEntry
    {
        public Guid GameId { get; set; }
        public string Name { get; set; }
        public long PlaytimeSeconds { get; set; }
        public int SessionCount { get; set; }

        /// <summary>Resolved local cover file path, or null if the game has none / it's unresolvable.</summary>
        public string CoverImagePath { get; set; }

        /// <summary>Deterministic placeholder shown in place of a cover when CoverImagePath is null.</summary>
        public string PlaceholderColorHex { get; set; }
        public string PlaceholderInitials { get; set; }

        /// <summary>1-based rank within whatever list this entry currently belongs to (set by the consumer, e.g. Most Played).</summary>
        public int Rank { get; set; }

        /// <summary>This entry's PlaytimeSeconds as a fraction (0-1) of the top entry's, for a relative-length progress bar. Set by the consumer.</summary>
        public double PlaytimeShareOfTop { get; set; }

        /// <summary>1-based rank this same game held in the prior year's full ranked list, or null if it wasn't played that year. Set by the consumer.</summary>
        public int? PreviousYearRank { get; set; }

        /// <summary>Positive = moved up (lower rank number) since last year, negative = moved down. Meaningless when Trend is New.</summary>
        public int RankDelta => PreviousYearRank.HasValue ? PreviousYearRank.Value - Rank : 0;

        public RankTrend Trend
        {
            get
            {
                if (!PreviousYearRank.HasValue)
                {
                    return RankTrend.New;
                }

                if (PreviousYearRank.Value == Rank)
                {
                    return RankTrend.Same;
                }

                return PreviousYearRank.Value > Rank ? RankTrend.Up : RankTrend.Down;
            }
        }

        public double AvgSessionSeconds => SessionCount > 0 ? (double)PlaytimeSeconds / SessionCount : 0;

        /// <summary>Resolved from PlayniteAchievements' own SQLite cache via IAchievementsProvider,
        /// scoped to only the games already in TopGames. Both stay null when PlayniteAchievements
        /// has no data for this game (or isn't installed) - never zeroed, so AchievementSummaryText
        /// can tell "no data" apart from "0 unlocked."</summary>
        public int? AchievementsUnlocked { get; set; }
        public int? AchievementsTotal { get; set; }

        /// <summary>Precomputed for direct XAML binding - "" when there's no PlayniteAchievements
        /// data for this game, otherwise " · 42/60 achievements". Follows the same "format text in
        /// code, not XAML" convention as HomeViewModel.BacklogDeltaText/GamingYearSummaryBuilder.</summary>
        public string AchievementSummaryText =>
            AchievementsTotal.HasValue && AchievementsTotal.Value > 0
                ? $" · {AchievementsUnlocked}/{AchievementsTotal} achievements"
                : "";
    }

    /// <summary>The single rarest achievement (lowest GlobalPercentUnlocked) unlocked within a
    /// given year - see RecapAggregator.BuildRecap. Null on RecapData when PlayniteAchievements
    /// isn't available or no rated achievement was unlocked that year.</summary>
    public class RarestAchievementEntry
    {
        public string GameName { get; set; }
        public string AchievementName { get; set; }
        public double GlobalPercentUnlocked { get; set; }
    }

    public class RecapData
    {
        public int Year { get; set; }
        public long TotalPlaytimeSeconds { get; set; }
        public int TotalSessions { get; set; }
        public int ActiveDays { get; set; }
        public List<GameRecapEntry> TopGames { get; set; } = new List<GameRecapEntry>();
        public List<GameRecapEntry> NewGamesThisYear { get; set; } = new List<GameRecapEntry>();

        /// <summary>Top 6 genres/libraries by (override-aware) playtime - see RecapAggregator.BuildCategoryBreakdown.</summary>
        public List<CategoryBreakdownEntry> GenreBreakdown { get; set; } = new List<CategoryBreakdownEntry>();

        /// <summary>Playtime by owning library plugin (Steam, GOG, Epic, Xbox, "Manually Added", ...) -
        /// replaced PlatformBreakdown on Home, since most PC-centric libraries have every game on the same
        /// single platform, making a by-platform breakdown a near-constant 100% bar. A by-Source breakdown
        /// was tried first but rejected - Game.Source is often unset for manually-imported games, leaving
        /// most of the library uncounted. Library (Game.PluginId) resolves for every game.</summary>
        public List<CategoryBreakdownEntry> LibraryBreakdown { get; set; } = new List<CategoryBreakdownEntry>();

        /// <summary>Count of achievements unlocked within this year, resolved via
        /// IAchievementsProvider - null when PlayniteAchievements isn't available (never 0, so the
        /// stat card can tell "no data" apart from "unlocked nothing"). Not computed by
        /// BuildAllTimeRecap - year-scoped only, per the locked-in integration scope.</summary>
        public int? AchievementsUnlockedCount { get; set; }

        /// <summary>The single rarest (lowest GlobalPercentUnlocked) achievement unlocked this
        /// year, or null when PlayniteAchievements isn't available or nothing rated was unlocked.</summary>
        public RarestAchievementEntry RarestAchievementThisYear { get; set; }
    }
}
