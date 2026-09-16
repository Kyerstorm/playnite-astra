using System;
using System.Linq;
using Astra.Models;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class TrendAggregationServiceTests
    {
        [Fact]
        public void BuildTrend_Day_OnePointPerCalendarDay()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 3, 10, 20, 0, 0), 3600);

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Day, new DateTime(2026, 3, 10), new DateTime(2026, 3, 13));

            Assert.Equal(3, result.Points.Count);
            Assert.Equal(3600, result.Points[0].PlaytimeSeconds);
            Assert.Equal(0, result.Points[1].PlaytimeSeconds);
            Assert.Equal(0, result.Points[2].PlaytimeSeconds);
        }

        [Fact]
        public void BuildTrend_Week_BucketsByMondayStartWeek()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            // 2026-03-16 is a Monday; both sessions fall in the same Mon-Sun week.
            db.InsertSession(game, new DateTime(2026, 3, 16, 9, 0, 0), 1800);
            db.InsertSession(game, new DateTime(2026, 3, 22, 22, 0, 0), 1800); // Sunday, same week

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Week, new DateTime(2026, 3, 16), new DateTime(2026, 3, 23));

            var week = Assert.Single(result.Points);
            Assert.Equal(3600, week.PlaytimeSeconds);
            Assert.Equal(new DateTime(2026, 3, 16), week.PeriodStart);
        }

        [Fact]
        public void BuildTrend_Month_BucketsByCalendarMonthNotRolling30Days()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 1, 31, 23, 0, 0), 1800);
            db.InsertSession(game, new DateTime(2026, 2, 1, 1, 0, 0), 1800);

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Month, new DateTime(2026, 1, 1), new DateTime(2026, 3, 1));

            Assert.Equal(2, result.Points.Count);
            Assert.Equal(1800, result.Points[0].PlaytimeSeconds); // January
            Assert.Equal(1800, result.Points[1].PlaytimeSeconds); // February
        }

        [Fact]
        public void BuildTrend_Year_BucketsByCalendarYear()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2024, 6, 1), 3600);
            db.InsertSession(game, new DateTime(2026, 6, 1), 7200);

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Year, new DateTime(2024, 1, 1), new DateTime(2027, 1, 1));

            Assert.Equal(3, result.Points.Count);
            Assert.Equal(3600, result.Points[0].PlaytimeSeconds); // 2024
            Assert.Equal(0, result.Points[1].PlaytimeSeconds);    // 2025
            Assert.Equal(7200, result.Points[2].PlaytimeSeconds); // 2026
        }

        [Fact]
        public void BuildTrend_MultipleSessionsSamePeriod_Sums()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 5, 1, 9, 0, 0), 1000);
            db.InsertSession(game, new DateTime(2026, 5, 1, 20, 0, 0), 2000);
            db.InsertSession(game, new DateTime(2026, 5, 1, 23, 30, 0), 500);

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Day, new DateTime(2026, 5, 1), new DateTime(2026, 5, 2));

            Assert.Equal(3500, Assert.Single(result.Points).PlaytimeSeconds);
        }

        [Fact]
        public void BuildTrend_SessionCrossingMidnight_AttributedToStartDayOnly()
        {
            // SessionTracker back-computes StartedAt from (now - elapsed): a session that "crosses
            // midnight" is still just one row with one StartedAt. It must land in exactly one day's
            // bucket (its start day), never split or double-counted across two.
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 4, 9, 23, 30, 0), 3600); // started 23:30, ran past midnight

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Day, new DateTime(2026, 4, 9), new DateTime(2026, 4, 11));

            Assert.Equal(3600, result.Points[0].PlaytimeSeconds); // Apr 9
            Assert.Equal(0, result.Points[1].PlaytimeSeconds);    // Apr 10
        }

        [Fact]
        public void BuildTrend_EmptyPeriodsWithinRange_RemainAsZeroPoints()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 1, 15), 3600);
            db.InsertSession(game, new DateTime(2026, 3, 15), 7200);

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Month, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));

            Assert.Equal(3, result.Points.Count);
            Assert.Equal(0, result.Points[1].PlaytimeSeconds); // February stays present at zero
        }

        [Fact]
        public void BuildTrend_EmptyRange_ReturnsZeroedResultWithoutThrowing()
        {
            var db = TestDatabaseFactory.CreateTemp();

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Month, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));

            Assert.Equal(0, result.TotalPlaytimeSeconds);
            Assert.Equal(0, result.ActivePeriods);
            Assert.Equal(3, result.TotalPeriods);
            Assert.All(result.Points, p => Assert.Equal(0, p.PlaytimeSeconds));
        }

        [Fact]
        public void BuildTrend_RangeBoundaries_StartInclusiveEndExclusive()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 1, 1, 0, 0, 0), 100); // exactly at range start
            db.InsertSession(game, new DateTime(2026, 2, 1, 0, 0, 0), 200); // exactly at range end - excluded

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Day, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));

            Assert.Equal(100, Assert.Single(result.Points).PlaytimeSeconds);
            Assert.Equal(100, result.TotalPlaytimeSeconds);
        }

        [Fact]
        public void BuildTrend_NoDuplicateCounting_EachSessionContributesToExactlyOneBucket()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 6, 15), 5000);

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Month, new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));

            Assert.Equal(5000, result.Points.Sum(p => p.PlaytimeSeconds));
            Assert.Equal(5000, result.TotalPlaytimeSeconds);
        }

        [Fact]
        public void BuildTrend_LocalDateInterpretation_NoTimezoneShiftAppliedToStoredTimestamps()
        {
            // StartedAt is stored/read back as local wall-clock time verbatim (AstraDatabase never
            // calls ToLocalTime/ToUniversalTime) - a session recorded at 00:30 local must bucket into
            // that same calendar day, not the previous day via some hidden UTC conversion.
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 7, 4, 0, 30, 0), 900);

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Day, new DateTime(2026, 7, 4), new DateTime(2026, 7, 5));

            Assert.Equal(900, Assert.Single(result.Points).PlaytimeSeconds);
        }

        [Fact]
        public void BuildTrend_LongDateRange_TenYearsOfYearlyBucketsWithoutError()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2020, 1, 1), 1000);
            db.InsertSession(game, new DateTime(2029, 12, 31), 2000);

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Year, new DateTime(2020, 1, 1), new DateTime(2030, 1, 1));

            Assert.Equal(10, result.Points.Count);
            Assert.Equal(3000, result.TotalPlaytimeSeconds);
            Assert.Equal(2, result.ActivePeriods);
        }

        [Fact]
        public void BuildTrend_AveragePlaytime_IsTotalOverTotalPeriodsNotActivePeriods()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 1, 10), 3600 * 4);

            var result = new TrendAggregationService(db)
                .BuildTrend(TrendGranularity.Month, new DateTime(2026, 1, 1), new DateTime(2026, 5, 1));

            Assert.Equal(4, result.TotalPeriods);
            Assert.Equal(1, result.ActivePeriods);
            Assert.Equal((3600 * 4) / 4.0, result.AveragePlaytimeSeconds);
        }

        [Fact]
        public void BuildMonthlyTrendForYear_MatchesBuildTrendForSameCalendarYear()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var game = Guid.NewGuid();
            db.InsertSession(game, new DateTime(2026, 8, 20), 1234);

            var service = new TrendAggregationService(db);
            var viaConvenience = service.BuildMonthlyTrendForYear(2026);
            var viaExplicitRange = service.BuildTrend(TrendGranularity.Month, new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));

            Assert.Equal(viaExplicitRange.TotalPlaytimeSeconds, viaConvenience.TotalPlaytimeSeconds);
            Assert.Equal(viaExplicitRange.Points.Count, viaConvenience.Points.Count);
        }
    }
}
