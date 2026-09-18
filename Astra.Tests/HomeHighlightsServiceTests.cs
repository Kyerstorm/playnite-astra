using System;
using System.Collections.Generic;
using Astra.Models;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class HomeHighlightsServiceTests
    {
        [Fact]
        public void FindMostImproved_ReturnsReturningGameWithLargestPositiveDelta()
        {
            var gameId = Guid.NewGuid();
            var current = new List<GameRecapEntry> { new GameRecapEntry { GameId = gameId, Name = "Grew", PlaytimeSeconds = 100 } };
            var previous = new List<GameRecapEntry> { new GameRecapEntry { GameId = gameId, Name = "Grew", PlaytimeSeconds = 40 } };

            var result = HomeHighlightsService.FindMostImproved(current, previous);

            Assert.NotNull(result);
            Assert.Equal(60, result.DeltaSeconds);
        }

        [Fact]
        public void FindMostImproved_ExcludesGameNotPresentInPreviousYear()
        {
            var newGameId = Guid.NewGuid();
            var current = new List<GameRecapEntry> { new GameRecapEntry { GameId = newGameId, Name = "Brand New", PlaytimeSeconds = 1000 } };
            var previous = new List<GameRecapEntry>();

            var result = HomeHighlightsService.FindMostImproved(current, previous);

            Assert.Null(result);
        }

        [Fact]
        public void FindMostImproved_ExcludesGameWithZeroPreviousPlaytime()
        {
            var gameId = Guid.NewGuid();
            var current = new List<GameRecapEntry> { new GameRecapEntry { GameId = gameId, Name = "Zeroed Override", PlaytimeSeconds = 500 } };
            var previous = new List<GameRecapEntry> { new GameRecapEntry { GameId = gameId, Name = "Zeroed Override", PlaytimeSeconds = 0 } };

            var result = HomeHighlightsService.FindMostImproved(current, previous);

            Assert.Null(result);
        }

        [Fact]
        public void FindMostImproved_ReturnsNullWhenNoReturningGameImproved()
        {
            var gameId = Guid.NewGuid();
            var current = new List<GameRecapEntry> { new GameRecapEntry { GameId = gameId, Name = "Declined", PlaytimeSeconds = 10 } };
            var previous = new List<GameRecapEntry> { new GameRecapEntry { GameId = gameId, Name = "Declined", PlaytimeSeconds = 100 } };

            var result = HomeHighlightsService.FindMostImproved(current, previous);

            Assert.Null(result);
        }

        [Fact]
        public void FindMostImproved_PicksLargerDeltaOverMultipleReturningGames()
        {
            var smallGrowth = Guid.NewGuid();
            var bigGrowth = Guid.NewGuid();
            var current = new List<GameRecapEntry>
            {
                new GameRecapEntry { GameId = smallGrowth, Name = "Small", PlaytimeSeconds = 20 },
                new GameRecapEntry { GameId = bigGrowth, Name = "Big", PlaytimeSeconds = 500 }
            };
            var previous = new List<GameRecapEntry>
            {
                new GameRecapEntry { GameId = smallGrowth, Name = "Small", PlaytimeSeconds = 10 },
                new GameRecapEntry { GameId = bigGrowth, Name = "Big", PlaytimeSeconds = 50 }
            };

            var result = HomeHighlightsService.FindMostImproved(current, previous);

            Assert.Equal("Big", result.Name);
        }
    }
}
