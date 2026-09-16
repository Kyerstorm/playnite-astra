using System;
using System.Collections.Generic;
using System.Globalization;
using Astra.Data;
using Astra.Models;

namespace Astra.Services
{
    /// <summary>
    /// Buckets Astra's own session history into a TrendResult for a given granularity and date
    /// range. Nothing here is persisted - recomputed on demand from a bounded database query,
    /// same "cheap enough to recompute" philosophy as RecapAggregator. This is the single source
    /// of trend math for both the Home mini-chart and the Trends page, so their numbers can never
    /// drift apart.
    /// </summary>
    public class TrendAggregationService
    {
        private readonly AstraDatabase database;

        public TrendAggregationService(AstraDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Buckets sessions between rangeStart (inclusive) and rangeEnd (exclusive) at the given
        /// granularity. Both bounds are snapped outward to whole bucket boundaries first, so every
        /// returned point represents a complete period - callers don't need to align them.
        /// </summary>
        public TrendResult BuildTrend(TrendGranularity granularity, DateTime rangeStart, DateTime rangeEnd)
        {
            var alignedStart = AlignDown(granularity, rangeStart);
            var alignedEnd = AlignUp(granularity, rangeEnd);

            var points = BuildEmptyBuckets(granularity, alignedStart, alignedEnd);
            var sessions = database.GetSessionsForDateRange(alignedStart, alignedEnd);
            var gamesPerBucket = new HashSet<Guid>[points.Count];

            foreach (var session in sessions)
            {
                var index = BucketIndex(granularity, alignedStart, session.StartedAt);
                if (index >= 0 && index < points.Count)
                {
                    points[index].PlaytimeSeconds += session.DurationSeconds;
                    points[index].SessionCount++;

                    if (gamesPerBucket[index] == null)
                    {
                        gamesPerBucket[index] = new HashSet<Guid>();
                    }
                    gamesPerBucket[index].Add(session.GameId);
                }
            }

            for (var i = 0; i < points.Count; i++)
            {
                points[i].ActiveGameCount = gamesPerBucket[i]?.Count ?? 0;
            }

            long total = 0;
            var activePeriods = 0;
            var sessionCount = 0;
            foreach (var point in points)
            {
                total += point.PlaytimeSeconds;
                sessionCount += point.SessionCount;
                if (point.PlaytimeSeconds > 0)
                {
                    activePeriods++;
                }
            }

            return new TrendResult
            {
                Granularity = granularity,
                StartDate = alignedStart,
                EndDate = alignedEnd,
                Points = points,
                TotalPlaytimeSeconds = total,
                AveragePlaytimeSeconds = points.Count > 0 ? (double)total / points.Count : 0,
                ActivePeriods = activePeriods,
                TotalPeriods = points.Count,
                SessionCount = sessionCount
            };
        }

        /// <summary>Convenience for Home's mini-chart and Trends' default Month view: monthly buckets
        /// spanning one calendar year. Both callers go through this so "Home year" and "Trends / Month /
        /// that year" are guaranteed to match - see CLAUDE.md sync requirement.</summary>
        public TrendResult BuildMonthlyTrendForYear(int year)
        {
            return BuildTrend(TrendGranularity.Month, new DateTime(year, 1, 1), new DateTime(year + 1, 1, 1));
        }

        private static DateTime AlignDown(TrendGranularity granularity, DateTime value)
        {
            switch (granularity)
            {
                case TrendGranularity.Day:
                    return value.Date;
                case TrendGranularity.Week:
                    return StartOfWeek(value.Date);
                case TrendGranularity.Month:
                    return new DateTime(value.Year, value.Month, 1);
                case TrendGranularity.Year:
                    return new DateTime(value.Year, 1, 1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(granularity));
            }
        }

        private static DateTime AlignUp(TrendGranularity granularity, DateTime value)
        {
            var flooredValue = AlignDown(granularity, value);
            // An exact bucket-boundary end (e.g. midnight on the 1st) is already aligned and shouldn't
            // pull in an extra empty trailing bucket.
            if (flooredValue == value)
            {
                return flooredValue;
            }

            switch (granularity)
            {
                case TrendGranularity.Day:
                    return flooredValue.AddDays(1);
                case TrendGranularity.Week:
                    return flooredValue.AddDays(7);
                case TrendGranularity.Month:
                    return flooredValue.AddMonths(1);
                case TrendGranularity.Year:
                    return flooredValue.AddYears(1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(granularity));
            }
        }

        /// <summary>Monday-start week, applied consistently everywhere Astra buckets by week - no
        /// existing convention was found in RecapAggregator/SessionTracker to reuse (see FEATURES.md/
        /// CLAUDE.md research notes), so ISO-8601 Monday-start was chosen since it's locale-independent
        /// (unlike CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek).</summary>
        private static DateTime StartOfWeek(DateTime date)
        {
            var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.AddDays(-diff);
        }

        private static int BucketIndex(TrendGranularity granularity, DateTime alignedStart, DateTime sessionStartedAt)
        {
            switch (granularity)
            {
                case TrendGranularity.Day:
                    return (int)(sessionStartedAt.Date - alignedStart).TotalDays;
                case TrendGranularity.Week:
                    return (int)((StartOfWeek(sessionStartedAt.Date) - alignedStart).TotalDays / 7);
                case TrendGranularity.Month:
                    return ((sessionStartedAt.Year - alignedStart.Year) * 12) + (sessionStartedAt.Month - alignedStart.Month);
                case TrendGranularity.Year:
                    return sessionStartedAt.Year - alignedStart.Year;
                default:
                    throw new ArgumentOutOfRangeException(nameof(granularity));
            }
        }

        private static List<TrendPoint> BuildEmptyBuckets(TrendGranularity granularity, DateTime alignedStart, DateTime alignedEnd)
        {
            var points = new List<TrendPoint>();
            var cursor = alignedStart;

            while (cursor < alignedEnd)
            {
                DateTime next;
                string label;

                switch (granularity)
                {
                    case TrendGranularity.Day:
                        next = cursor.AddDays(1);
                        label = cursor.ToString("MMM d", CultureInfo.InvariantCulture);
                        break;
                    case TrendGranularity.Week:
                        next = cursor.AddDays(7);
                        label = "W" + CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(cursor, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                        break;
                    case TrendGranularity.Month:
                        next = cursor.AddMonths(1);
                        label = cursor.ToString("MMM", CultureInfo.InvariantCulture);
                        break;
                    case TrendGranularity.Year:
                        next = cursor.AddYears(1);
                        label = cursor.Year.ToString(CultureInfo.InvariantCulture);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(granularity));
                }

                points.Add(new TrendPoint
                {
                    PeriodStart = cursor,
                    PeriodEnd = next,
                    Label = label,
                    PlaytimeSeconds = 0
                });

                cursor = next;
            }

            return points;
        }
    }
}
