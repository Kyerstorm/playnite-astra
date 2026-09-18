using System;
using System.Collections.Generic;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class BacklogBurndownServiceTests
    {
        [Fact]
        public void Compute_GameWithNoAddedDate_CountsAsOwnedInBothSnapshots()
        {
            var gameId = Guid.NewGuid();
            var games = new List<GameInfo> { new GameInfo { Id = gameId, Name = "No Added Date", Added = null } };
            var currentCutoff = new DateTime(2027, 1, 1);
            var previousCutoff = new DateTime(2026, 1, 1);

            var result = BacklogBurndownService.Compute(games, new HashSet<Guid>(), new HashSet<Guid>(), currentCutoff, previousCutoff);

            Assert.Equal(1, result.OwnedGameCount);
            Assert.Equal(100, result.BacklogPercent);
            Assert.Equal(100, result.PreviousYearBacklogPercent);
        }

        [Fact]
        public void Compute_GameAddedBetweenCutoffs_OwnedNowButExcludedFromPreviousDenominator()
        {
            var gameId = Guid.NewGuid();
            var games = new List<GameInfo> { new GameInfo { Id = gameId, Name = "Newly Added", Added = new DateTime(2026, 6, 1) } };
            var currentCutoff = new DateTime(2027, 1, 1);
            var previousCutoff = new DateTime(2026, 1, 1);

            var result = BacklogBurndownService.Compute(games, new HashSet<Guid>(), new HashSet<Guid>(), currentCutoff, previousCutoff);

            Assert.Equal(1, result.OwnedGameCount);
            Assert.Equal(0, result.PreviousYearBacklogPercent);
        }

        [Fact]
        public void Compute_NoOwnedGames_BacklogPercentIsZeroNotNaN()
        {
            var result = BacklogBurndownService.Compute(
                new List<GameInfo>(), new HashSet<Guid>(), new HashSet<Guid>(),
                new DateTime(2027, 1, 1), new DateTime(2026, 1, 1));

            Assert.Equal(0, result.OwnedGameCount);
            Assert.Equal(0, result.BacklogPercent);
        }

        [Fact]
        public void Compute_BacklogShrank_DeltaIsNegative()
        {
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            var games = new List<GameInfo>
            {
                new GameInfo { Id = a, Name = "A", Added = new DateTime(2020, 1, 1) },
                new GameInfo { Id = b, Name = "B", Added = new DateTime(2020, 1, 1) }
            };
            // Previously neither was played (100% backlog); now both have been played (0% backlog).
            var playedNow = new HashSet<Guid> { a, b };
            var playedBefore = new HashSet<Guid>();

            var result = BacklogBurndownService.Compute(games, playedNow, playedBefore, new DateTime(2027, 1, 1), new DateTime(2026, 1, 1));

            Assert.True(result.DeltaPercentagePoints < 0);
        }

        [Fact]
        public void Compute_BacklogGrew_DeltaIsPositive()
        {
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            var games = new List<GameInfo>
            {
                new GameInfo { Id = a, Name = "A", Added = new DateTime(2020, 1, 1) },
                new GameInfo { Id = b, Name = "B", Added = new DateTime(2020, 1, 1) }
            };
            // Previously both were played (0% backlog); now neither has been played as of the
            // (hypothetical) current cutoff's played set - backlog grew.
            var playedNow = new HashSet<Guid>();
            var playedBefore = new HashSet<Guid> { a, b };

            var result = BacklogBurndownService.Compute(games, playedNow, playedBefore, new DateTime(2027, 1, 1), new DateTime(2026, 1, 1));

            Assert.True(result.DeltaPercentagePoints > 0);
        }

        [Fact]
        public void Compute_PlayedGame_ExcludedFromUnplayedCount()
        {
            var playedId = Guid.NewGuid();
            var unplayedId = Guid.NewGuid();
            var games = new List<GameInfo>
            {
                new GameInfo { Id = playedId, Name = "Played", Added = new DateTime(2020, 1, 1) },
                new GameInfo { Id = unplayedId, Name = "Unplayed", Added = new DateTime(2020, 1, 1) }
            };
            var played = new HashSet<Guid> { playedId };

            var result = BacklogBurndownService.Compute(games, played, played, new DateTime(2027, 1, 1), new DateTime(2026, 1, 1));

            Assert.Equal(1, result.UnplayedGameCount);
        }
    }
}
