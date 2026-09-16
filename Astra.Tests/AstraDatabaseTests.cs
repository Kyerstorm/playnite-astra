using System;
using System.Collections.Generic;
using Xunit;

namespace Astra.Tests
{
    public class AstraDatabaseTests
    {
        [Fact]
        public void GetEarliestSessionDates_ReturnsMinStartedAtPerGame()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameA = Guid.NewGuid();
            var gameB = Guid.NewGuid();
            db.InsertSession(gameA, new DateTime(2026, 3, 1), 100);
            db.InsertSession(gameA, new DateTime(2026, 1, 1), 100); // earlier - should win
            db.InsertSession(gameB, new DateTime(2025, 6, 1), 100);

            var result = db.GetEarliestSessionDates(new[] { gameA, gameB });

            Assert.Equal(new DateTime(2026, 1, 1), result[gameA]);
            Assert.Equal(new DateTime(2025, 6, 1), result[gameB]);
        }

        [Fact]
        public void GetEarliestSessionDates_IgnoresGamesNotInRequestedSet()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var requested = Guid.NewGuid();
            var other = Guid.NewGuid();
            db.InsertSession(requested, new DateTime(2026, 1, 1), 100);
            db.InsertSession(other, new DateTime(2026, 1, 1), 100);

            var result = db.GetEarliestSessionDates(new[] { requested });

            Assert.Single(result);
            Assert.True(result.ContainsKey(requested));
        }

        [Fact]
        public void GetEarliestSessionDates_EmptyInput_ReturnsEmptyWithoutQuerying()
        {
            var db = TestDatabaseFactory.CreateTemp();

            var result = db.GetEarliestSessionDates(new List<Guid>());

            Assert.Empty(result);
        }

        [Fact]
        public void GetEarliestSessionDates_NullInput_ReturnsEmpty()
        {
            var db = TestDatabaseFactory.CreateTemp();

            var result = db.GetEarliestSessionDates(null);

            Assert.Empty(result);
        }
    }
}
