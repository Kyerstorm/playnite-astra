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
    }

    public class RecapData
    {
        public int Year { get; set; }
        public long TotalPlaytimeSeconds { get; set; }
        public int TotalSessions { get; set; }
        public int ActiveDays { get; set; }
        public List<GameRecapEntry> TopGames { get; set; } = new List<GameRecapEntry>();
        public List<GameRecapEntry> NewGamesThisYear { get; set; } = new List<GameRecapEntry>();

        /// <summary>Top 6 genres/platforms by (override-aware) playtime - see RecapAggregator.BuildCategoryBreakdown.</summary>
        public List<CategoryBreakdownEntry> GenreBreakdown { get; set; } = new List<CategoryBreakdownEntry>();
        public List<CategoryBreakdownEntry> PlatformBreakdown { get; set; } = new List<CategoryBreakdownEntry>();
    }
}
