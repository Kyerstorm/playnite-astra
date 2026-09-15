using System.Collections.Generic;
using System.Linq;
using Astra.Models;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class LeaderboardSorterTests
    {
        private static List<GameRecapEntry> Entries()
        {
            return new List<GameRecapEntry>
            {
                // 2h total across 2 sessions -> 60m avg
                new GameRecapEntry { Name = "Few Long Sessions", PlaytimeSeconds = 7200, SessionCount = 2 },
                // 1h total across 10 sessions -> 6m avg, but most sessions
                new GameRecapEntry { Name = "Many Short Sessions", PlaytimeSeconds = 3600, SessionCount = 10 },
                // Never actually launched (e.g. a manual playtime override with no tracked sessions)
                new GameRecapEntry { Name = "Never Played", PlaytimeSeconds = 0, SessionCount = 0 }
            };
        }

        [Fact]
        public void Sort_Playtime_OrdersDescendingByPlaytimeSeconds()
        {
            var result = LeaderboardSorter.Sort(Entries(), LeaderboardSortMode.Playtime);

            Assert.Equal(new[] { "Few Long Sessions", "Many Short Sessions", "Never Played" }, result.Select(e => e.Name));
        }

        [Fact]
        public void Sort_Sessions_OrdersDescendingBySessionCount()
        {
            var result = LeaderboardSorter.Sort(Entries(), LeaderboardSortMode.Sessions);

            Assert.Equal(new[] { "Many Short Sessions", "Few Long Sessions", "Never Played" }, result.Select(e => e.Name));
        }

        [Fact]
        public void Sort_AverageSessionLength_OrdersDescendingByAvgSessionSeconds_AndDoesNotThrowOnZeroSessions()
        {
            var result = LeaderboardSorter.Sort(Entries(), LeaderboardSortMode.AverageSessionLength);

            Assert.Equal(new[] { "Few Long Sessions", "Many Short Sessions", "Never Played" }, result.Select(e => e.Name));
            Assert.Equal(0, result.Last().AvgSessionSeconds);
        }
    }
}
