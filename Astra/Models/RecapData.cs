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
