using System;
using Xunit;

namespace Astra.Tests
{
    public class PlaytimeOverrideTests
    {
        [Fact]
        public void SetPlaytimeOverride_ThenGet_ReturnsIt()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();

            db.SetPlaytimeOverride(gameId, 2026, 31680); // 8.8h

            var overrides = db.GetPlaytimeOverridesForYear(2026);
            Assert.Equal(31680, overrides[gameId]);
        }

        [Fact]
        public void SetPlaytimeOverride_CalledTwiceForSameGameYear_UpdatesRatherThanDuplicates()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();

            db.SetPlaytimeOverride(gameId, 2026, 1000);
            db.SetPlaytimeOverride(gameId, 2026, 2000);

            var overrides = db.GetPlaytimeOverridesForYear(2026);
            Assert.Single(overrides);
            Assert.Equal(2000, overrides[gameId]);
        }

        [Fact]
        public void GetPlaytimeOverridesForYear_OnlyReturnsThatYearsOverrides()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();

            db.SetPlaytimeOverride(gameId, 2025, 1000);
            db.SetPlaytimeOverride(gameId, 2026, 2000);

            var overrides = db.GetPlaytimeOverridesForYear(2026);
            Assert.Single(overrides);
            Assert.Equal(2000, overrides[gameId]);
        }

        [Fact]
        public void ClearPlaytimeOverride_RemovesIt()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();
            db.SetPlaytimeOverride(gameId, 2026, 1000);

            db.ClearPlaytimeOverride(gameId, 2026);

            Assert.Empty(db.GetPlaytimeOverridesForYear(2026));
        }

        [Fact]
        public void ClearAllData_AlsoClearsPlaytimeOverrides()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();
            db.SetPlaytimeOverride(gameId, 2026, 1000);

            db.ClearAllData();

            Assert.Empty(db.GetPlaytimeOverridesForYear(2026));
        }
    }
}
