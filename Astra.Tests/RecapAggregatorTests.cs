using System;
using System.Collections.Generic;
using System.Linq;
using Astra.Services;
using Astra.Tests.Fakes;
using Xunit;

namespace Astra.Tests
{
    public class RecapAggregatorTests
    {
        [Fact]
        public void BuildRecap_TotalsAndTopGames_ReflectOnlySelectedYear()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameA = Guid.NewGuid();
            var gameB = Guid.NewGuid();

            db.InsertSession(gameA, new DateTime(2026, 3, 1), 3600);
            db.InsertSession(gameA, new DateTime(2026, 3, 2), 1800);
            db.InsertSession(gameB, new DateTime(2026, 5, 1), 7200);
            db.InsertSession(gameB, new DateTime(2025, 12, 31), 999999); // previous year, must be excluded

            var games = new FakeGameInfoProvider()
                .Add(new GameInfo { Id = gameA, Name = "Game A" })
                .Add(new GameInfo { Id = gameB, Name = "Game B" });

            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            Assert.Equal(3600 + 1800 + 7200, recap.TotalPlaytimeSeconds);
            Assert.Equal(3, recap.TotalSessions);
            Assert.Equal("Game B", recap.TopGames.First().Name); // 7200s beats gameA's 5400s combined
        }

        [Fact]
        public void BuildRecap_NewGamesThisYear_MatchesGamesAddedInThatCalendarYear()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var newGame = Guid.NewGuid();
            var oldGame = Guid.NewGuid();

            var games = new FakeGameInfoProvider()
                .Add(new GameInfo { Id = newGame, Name = "New Game", Added = new DateTime(2026, 4, 10) })
                .Add(new GameInfo { Id = oldGame, Name = "Old Game", Added = new DateTime(2019, 1, 1) });

            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            var newGameName = Assert.Single(recap.NewGamesThisYear).Name;
            Assert.Equal("New Game", newGameName);
        }

        [Fact]
        public void BuildRecap_NewGamesThisYear_OrderedByAddedDateNewestFirst()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var earlierAdd = Guid.NewGuid();
            var laterAdd = Guid.NewGuid();

            // earlierAdd has far more playtime than laterAdd, so an ordering bug that
            // sorts by playtime instead of Added date would put earlierAdd first.
            db.InsertSession(earlierAdd, new DateTime(2026, 2, 1), 100 * 3600);
            db.InsertSession(laterAdd, new DateTime(2026, 11, 1), 3600);

            var games = new FakeGameInfoProvider()
                .Add(new GameInfo { Id = earlierAdd, Name = "Added In January", Added = new DateTime(2026, 1, 5) })
                .Add(new GameInfo { Id = laterAdd, Name = "Added In November", Added = new DateTime(2026, 11, 1) });

            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            Assert.Equal("Added In November", recap.NewGamesThisYear.First().Name);
            Assert.Equal("Added In January", recap.NewGamesThisYear.Last().Name);
        }

        [Fact]
        public void BuildRecap_UnknownGameIdInSessions_FallsBackToPlaceholderName()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var orphanGameId = Guid.NewGuid();
            db.InsertSession(orphanGameId, new DateTime(2026, 1, 1), 600);

            var recap = new RecapAggregator(db, new FakeGameInfoProvider()).BuildRecap(2026);

            Assert.Equal("Unknown game", recap.TopGames.Single().Name);
        }

        [Fact]
        public void BuildRecap_NoSessions_ReturnsZeroedRecapWithoutThrowing()
        {
            var db = TestDatabaseFactory.CreateTemp();

            var recap = new RecapAggregator(db, new FakeGameInfoProvider()).BuildRecap(2026);

            Assert.Equal(0, recap.TotalPlaytimeSeconds);
            Assert.Empty(recap.TopGames);
        }

        [Fact]
        public void BuildRecap_WithOverride_ReplacesComputedPlaytimeForThatGame()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();
            db.InsertSession(gameId, new DateTime(2026, 3, 1), 108 * 3600 + 48 * 60); // ~108.8h tracked

            db.SetPlaytimeOverride(gameId, 2026, (long)(8.8 * 3600));

            var games = new FakeGameInfoProvider().Add(new GameInfo { Id = gameId, Name = "Monster Hunter Wilds" });
            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            var entry = Assert.Single(recap.TopGames);
            Assert.Equal((long)(8.8 * 3600), entry.PlaytimeSeconds);
            Assert.Equal(1, entry.SessionCount); // real tracked session count is untouched by the override
        }

        [Fact]
        public void BuildRecap_WithOverride_TotalPlaytimeReflectsOverriddenValue()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameA = Guid.NewGuid();
            var gameB = Guid.NewGuid();
            db.InsertSession(gameA, new DateTime(2026, 1, 1), 100 * 3600);
            db.InsertSession(gameB, new DateTime(2026, 1, 1), 50 * 3600);
            db.SetPlaytimeOverride(gameA, 2026, 10 * 3600);

            var games = new FakeGameInfoProvider()
                .Add(new GameInfo { Id = gameA, Name = "Game A" })
                .Add(new GameInfo { Id = gameB, Name = "Game B" });
            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            Assert.Equal(10 * 3600 + 50 * 3600, recap.TotalPlaytimeSeconds);
        }

        [Fact]
        public void BuildRecap_OverrideCanChangeTopGamesRankingOrder()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var leader = Guid.NewGuid();
            var underdog = Guid.NewGuid();
            db.InsertSession(leader, new DateTime(2026, 1, 1), 100 * 3600);
            db.InsertSession(underdog, new DateTime(2026, 1, 1), 10 * 3600);
            db.SetPlaytimeOverride(underdog, 2026, 200 * 3600); // now the real leader

            var games = new FakeGameInfoProvider()
                .Add(new GameInfo { Id = leader, Name = "Former Leader" })
                .Add(new GameInfo { Id = underdog, Name = "New Leader" });
            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            Assert.Equal("New Leader", recap.TopGames.First().Name);
        }

        [Fact]
        public void BuildRecap_GameWithoutOverride_UsesComputedSumUnaffected()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var editedGame = Guid.NewGuid();
            var untouchedGame = Guid.NewGuid();
            db.InsertSession(editedGame, new DateTime(2026, 1, 1), 100 * 3600);
            db.InsertSession(untouchedGame, new DateTime(2026, 1, 1), 20 * 3600);
            db.SetPlaytimeOverride(editedGame, 2026, 5 * 3600);

            var games = new FakeGameInfoProvider()
                .Add(new GameInfo { Id = editedGame, Name = "Edited" })
                .Add(new GameInfo { Id = untouchedGame, Name = "Untouched" });
            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            var untouchedEntry = recap.TopGames.Single(e => e.GameId == untouchedGame);
            Assert.Equal(20 * 3600, untouchedEntry.PlaytimeSeconds);
        }

        [Fact]
        public void BuildRecap_LongestSession_ReturnsSessionWithMaxDuration()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();
            db.InsertSession(gameId, new DateTime(2026, 3, 1), 1800);
            db.InsertSession(gameId, new DateTime(2026, 3, 3), 22320); // 6.2h, the longest

            var games = new FakeGameInfoProvider().Add(new GameInfo { Id = gameId, Name = "Baldur's Gate 3" });
            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            Assert.Equal(22320, recap.LongestSession.DurationSeconds);
            Assert.Equal("Baldur's Gate 3", recap.LongestSession.GameName);
            Assert.Equal(new DateTime(2026, 3, 3), recap.LongestSession.StartedAt);
        }

        [Fact]
        public void BuildRecap_LongestSession_NullWhenNoSessions()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var recap = new RecapAggregator(db, new FakeGameInfoProvider()).BuildRecap(2026);

            Assert.Null(recap.LongestSession);
        }

        [Fact]
        public void BuildRecap_GenreBreakdown_SumsPlaytimeAcrossGamesSharingAGenre()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var rpgA = Guid.NewGuid();
            var rpgB = Guid.NewGuid();
            db.InsertSession(rpgA, new DateTime(2026, 1, 1), 3600);
            db.InsertSession(rpgB, new DateTime(2026, 1, 1), 1800);

            var games = new FakeGameInfoProvider()
                .Add(new GameInfo { Id = rpgA, Name = "A", Genres = new List<string> { "RPG" } })
                .Add(new GameInfo { Id = rpgB, Name = "B", Genres = new List<string> { "RPG" } });

            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            var rpgEntry = Assert.Single(recap.GenreBreakdown);
            Assert.Equal("RPG", rpgEntry.Label);
            Assert.Equal(3600 + 1800, rpgEntry.PlaytimeSeconds);
        }

        [Fact]
        public void BuildRecap_GenreBreakdown_MultiGenreGame_CountsFullyUnderEachGenre()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();
            db.InsertSession(gameId, new DateTime(2026, 1, 1), 3600);

            var games = new FakeGameInfoProvider()
                .Add(new GameInfo { Id = gameId, Name = "Hybrid", Genres = new List<string> { "RPG", "Action" } });

            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            Assert.Equal(2, recap.GenreBreakdown.Count);
            Assert.All(recap.GenreBreakdown, e => Assert.Equal(3600, e.PlaytimeSeconds));
        }

        [Fact]
        public void BuildRecap_GenreBreakdown_GameWithNoGenres_ExcludedFromBreakdown()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();
            db.InsertSession(gameId, new DateTime(2026, 1, 1), 3600);

            var games = new FakeGameInfoProvider().Add(new GameInfo { Id = gameId, Name = "Uncategorized" });
            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            Assert.Empty(recap.GenreBreakdown);
        }

        [Fact]
        public void BuildRecap_PlatformBreakdown_SumsPlaytimeByPlatform()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();
            db.InsertSession(gameId, new DateTime(2026, 1, 1), 7200);

            var games = new FakeGameInfoProvider()
                .Add(new GameInfo { Id = gameId, Name = "PC Game", Platforms = new List<string> { "PC" } });

            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            var pcEntry = Assert.Single(recap.PlatformBreakdown);
            Assert.Equal("PC", pcEntry.Label);
            Assert.Equal(7200, pcEntry.PlaytimeSeconds);
        }

        [Fact]
        public void BuildRecap_GenreBreakdown_OrderedDescendingAndCappedAtSix()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var games = new FakeGameInfoProvider();

            for (var i = 0; i < 8; i++)
            {
                var gameId = Guid.NewGuid();
                var seconds = (8 - i) * 3600; // strictly descending per genre
                db.InsertSession(gameId, new DateTime(2026, 1, 1), seconds);
                games.Add(new GameInfo { Id = gameId, Name = $"Game {i}", Genres = new List<string> { $"Genre{i}" } });
            }

            var recap = new RecapAggregator(db, games).BuildRecap(2026);

            Assert.Equal(6, recap.GenreBreakdown.Count);
            Assert.Equal("Genre0", recap.GenreBreakdown.First().Label); // 8h, the largest
        }
    }
}
