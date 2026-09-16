using System;
using Astra.Models;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class PlaytimeInsightsServiceTests
    {
        [Fact]
        public void Analyze_EmptyRange_ReturnsZeroedStatsWithoutThrowing()
        {
            var db = TestDatabaseFactory.CreateTemp();

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

            Assert.Null(result.Rhythm.BusiestDayOfWeek);
            Assert.Null(result.Rhythm.BusiestMonth);
            Assert.Equal(0, result.Rhythm.LongestStreakDays);
            Assert.Equal(0, result.Rhythm.CurrentStreakDays);
            Assert.Equal(0, result.Sessions.TotalSessions);
            Assert.Equal(0, result.Sessions.AverageSessionsPerActiveDay);
        }

        [Fact]
        public void Rhythm_BusiestDayOfWeek_PicksHighestTotalNotHighestCount()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            // 2026-03-16 is a Monday, 2026-03-17 a Tuesday.
            db.InsertSession(game, new DateTime(2026, 3, 16, 9, 0, 0), 3600); // Monday, 1 long session
            db.InsertSession(game, new DateTime(2026, 3, 17, 9, 0, 0), 600);
            db.InsertSession(game, new DateTime(2026, 3, 17, 12, 0, 0), 600); // Tuesday, 2 short sessions

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 3, 1), new DateTime(2026, 4, 1));

            Assert.Equal(DayOfWeek.Monday, result.Rhythm.BusiestDayOfWeek);
            Assert.Equal(3600, result.Rhythm.BusiestDayOfWeekSeconds);
        }

        [Fact]
        public void Rhythm_BusiestMonth_AggregatesAcrossYears()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2024, 9, 1), 1000);
            db.InsertSession(game, new DateTime(2025, 9, 1), 2000); // September total: 3000
            db.InsertSession(game, new DateTime(2025, 6, 1), 2500); // June total: 2500

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2024, 1, 1), new DateTime(2026, 1, 1));

            Assert.Equal(9, result.Rhythm.BusiestMonth);
            Assert.Equal(3000, result.Rhythm.BusiestMonthSeconds);
        }

        [Fact]
        public void Rhythm_LongestStreak_CountsConsecutiveCalendarDaysOnly()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 5, 1), 100);
            db.InsertSession(game, new DateTime(2026, 5, 2), 100);
            db.InsertSession(game, new DateTime(2026, 5, 3), 100);
            db.InsertSession(game, new DateTime(2026, 5, 5), 100); // gap on the 4th breaks the streak
            db.InsertSession(game, new DateTime(2026, 5, 6), 100);

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 5, 1), new DateTime(2026, 6, 1));

            Assert.Equal(3, result.Rhythm.LongestStreakDays);
        }

        [Fact]
        public void Rhythm_CurrentStreak_EndsOnMostRecentActiveDayInRange()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 5, 1), 100);
            db.InsertSession(game, new DateTime(2026, 5, 4), 100);
            db.InsertSession(game, new DateTime(2026, 5, 5), 100);
            db.InsertSession(game, new DateTime(2026, 5, 6), 100);

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 5, 1), new DateTime(2026, 6, 1));

            Assert.Equal(3, result.Rhythm.CurrentStreakDays); // 4th, 5th, 6th
        }

        [Fact]
        public void Rhythm_CurrentStreak_SingleActiveDayIsStreakOfOne()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 5, 1), 100);

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 5, 1), new DateTime(2026, 6, 1));

            Assert.Equal(1, result.Rhythm.CurrentStreakDays);
            Assert.Equal(1, result.Rhythm.LongestStreakDays);
        }

        [Fact]
        public void SessionStats_LongestAndShortest_ReflectIndividualSessionsNotAverages()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 5, 1, 8, 0, 0), 480);
            db.InsertSession(game, new DateTime(2026, 5, 1, 20, 0, 0), 28080);

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 5, 1), new DateTime(2026, 5, 2));

            Assert.Equal(2, result.Sessions.TotalSessions);
            Assert.Equal(28080, result.Sessions.LongestSessionSeconds);
            Assert.Equal(480, result.Sessions.ShortestSessionSeconds);
            Assert.Equal(14280, result.Sessions.AverageSessionSeconds);
        }

        [Fact]
        public void SessionStats_AverageSessionsPerActiveDay_DividesByDistinctDaysNotTotalDays()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 5, 1, 8, 0, 0), 100);
            db.InsertSession(game, new DateTime(2026, 5, 1, 20, 0, 0), 100);
            db.InsertSession(game, new DateTime(2026, 5, 3, 8, 0, 0), 100);

            // Range spans 31 days but only 2 of them have any activity.
            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 5, 1), new DateTime(2026, 6, 1));

            Assert.Equal(3.0 / 2.0, result.Sessions.AverageSessionsPerActiveDay);
        }
    }
}
