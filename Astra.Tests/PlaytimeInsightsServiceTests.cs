using System;
using System.Linq;
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
        public void Weekday_AlwaysReturnsSevenDaysMondayFirst()
        {
            var db = TestDatabaseFactory.CreateTemp();

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

            Assert.Equal(7, result.Weekday.Days.Count);
            Assert.Equal(DayOfWeek.Monday, result.Weekday.Days[0].DayOfWeek);
            Assert.Equal(DayOfWeek.Sunday, result.Weekday.Days[6].DayOfWeek);
        }

        [Fact]
        public void Weekday_SumsPlaytimeIntoCorrectDayRegardlessOfWhichCalendarWeek()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            // Two different Saturdays, three weeks apart.
            db.InsertSession(game, new DateTime(2026, 3, 7), 1000); // Saturday
            db.InsertSession(game, new DateTime(2026, 3, 28), 2000); // Saturday

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 3, 1), new DateTime(2026, 4, 1));

            var saturday = result.Weekday.Days.Single(d => d.DayOfWeek == DayOfWeek.Saturday);
            Assert.Equal(3000, saturday.PlaytimeSeconds);
            Assert.Equal(2, saturday.SessionCount);
        }

        [Theory]
        [InlineData(0, "< 30m")]
        [InlineData(1799, "< 30m")]
        [InlineData(1800, "30m-1h")]
        [InlineData(3599, "30m-1h")]
        [InlineData(3600, "1-2h")]
        [InlineData(7199, "1-2h")]
        [InlineData(7200, "2-4h")]
        [InlineData(14399, "2-4h")]
        [InlineData(14400, "4h+")]
        [InlineData(50000, "4h+")]
        public void SessionLengthBuckets_BoundariesAreInclusiveLowExclusiveHigh(long durationSeconds, string expectedLabel)
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 5, 1), durationSeconds);

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 5, 1), new DateTime(2026, 5, 2));

            var bucket = result.SessionLengths.Single(b => b.SessionCount == 1);
            Assert.Equal(expectedLabel, bucket.Label);
        }

        [Fact]
        public void SessionLengthBuckets_PercentagesSumToOneHundred()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 5, 1, 1, 0, 0), 100);
            db.InsertSession(game, new DateTime(2026, 5, 1, 2, 0, 0), 2000);
            db.InsertSession(game, new DateTime(2026, 5, 1, 3, 0, 0), 5000);

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 5, 1), new DateTime(2026, 5, 2));

            Assert.Equal(5, result.SessionLengths.Count);
            Assert.Equal(100.0, result.SessionLengths.Sum(b => b.Percentage), 5);
        }

        [Fact]
        public void SessionLengthBuckets_EmptyRange_AllZeroPercentNotNaN()
        {
            var db = TestDatabaseFactory.CreateTemp();

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

            Assert.All(result.SessionLengths, b => Assert.Equal(0, b.Percentage));
            Assert.All(result.SessionLengths, b => Assert.Equal(0, b.SessionCount));
        }

        [Fact]
        public void Streaks_MostActiveWeek_PicksHighestTotalPlaytimeWeek()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 3, 2), 1000); // week of Mon 2026-03-02
            db.InsertSession(game, new DateTime(2026, 3, 9), 5000); // week of Mon 2026-03-09

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 3, 1), new DateTime(2026, 4, 1));

            Assert.Equal(new DateTime(2026, 3, 9), result.Streaks.MostActiveWeekStart);
            Assert.Equal(5000, result.Streaks.MostActiveWeekSeconds);
        }

        [Fact]
        public void Streaks_EmptyRange_NullMostActiveWeekAndZeroStreaks()
        {
            var db = TestDatabaseFactory.CreateTemp();

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

            Assert.Null(result.Streaks.MostActiveWeekStart);
            Assert.Equal(0, result.Streaks.LongestStreakDays);
            Assert.Equal(0, result.Streaks.CurrentStreakDays);
        }

        [Fact]
        public void Hours_AlwaysReturns24Buckets()
        {
            var db = TestDatabaseFactory.CreateTemp();

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

            Assert.Equal(24, result.Hours.Hours.Count);
            Assert.Equal(Enumerable.Range(0, 24), result.Hours.Hours.Select(h => h.Hour));
        }

        [Fact]
        public void Hours_BucketsBySessionStartHour()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 5, 1, 21, 30, 0), 3600); // starts hour 21
            db.InsertSession(game, new DateTime(2026, 5, 1, 21, 45, 0), 1800); // also hour 21

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 5, 1), new DateTime(2026, 5, 2));

            Assert.Equal(5400, result.Hours.Hours[21].PlaytimeSeconds);
            Assert.Equal(2, result.Hours.Hours[21].SessionCount);
            Assert.Equal(1.0, result.Hours.Hours[21].BarFraction);
        }

        [Fact]
        public void WeekdayHourHeatmap_Always56Cells_MondayFirstEightBucketsPerDay()
        {
            var db = TestDatabaseFactory.CreateTemp();

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

            Assert.Equal(56, result.WeekdayHours.Cells.Count);
            Assert.Equal(DayOfWeek.Monday, result.WeekdayHours.Cells[0].DayOfWeek);
            Assert.Equal(0, result.WeekdayHours.Cells[0].BucketStartHour);
            Assert.Equal(3, result.WeekdayHours.Cells[0].BucketEndHour);
            Assert.Equal(21, result.WeekdayHours.Cells[7].BucketStartHour);
            Assert.Equal(24, result.WeekdayHours.Cells[7].BucketEndHour);
        }

        [Fact]
        public void WeekdayHourHeatmap_AttributesSessionToCorrectDayAndBucket()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            // 2026-03-21 is a Saturday, 20:15 falls in the 18-21 bucket.
            db.InsertSession(game, new DateTime(2026, 3, 21, 20, 15, 0), 1800);

            var result = new PlaytimeInsightsService(db).Analyze(new DateTime(2026, 3, 1), new DateTime(2026, 4, 1));

            var cell = result.WeekdayHours.Cells.Single(c => c.DayOfWeek == DayOfWeek.Saturday && c.BucketStartHour == 18);
            Assert.Equal(1800, cell.PlaytimeSeconds);
            Assert.Equal(1, cell.SessionCount);
            Assert.True(cell.HasActivity);
            Assert.Equal(1.0, cell.Intensity);
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
