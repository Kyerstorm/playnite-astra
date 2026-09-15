using System;
using System.Collections.Generic;

namespace Astra.Models
{
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
    }
}
