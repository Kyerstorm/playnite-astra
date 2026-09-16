using System;
using System.Collections.Generic;
using System.Linq;
using Astra.Data;
using Astra.Models;

namespace Astra.Services
{
    /// <summary>
    /// Computes the non-chart Trends analytics (Gaming Rhythm, Session Activity, and further
    /// sections added in later phases) for a date range. Like TrendAggregationService, this is
    /// recomputed on demand from a single bounded database query per call - never the full
    /// Sessions table - and holds no state of its own between calls.
    /// </summary>
    public class PlaytimeInsightsService
    {
        private readonly AstraDatabase database;

        public PlaytimeInsightsService(AstraDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>Builds every analytics section for [start, end) from one session fetch.</summary>
        public TrendAnalyticsResult Analyze(DateTime start, DateTime end)
        {
            var sessions = database.GetSessionsForDateRange(start, end);

            return new TrendAnalyticsResult
            {
                Rhythm = BuildRhythm(sessions),
                Sessions = BuildSessionStats(sessions),
                Weekday = BuildWeekdayStats(sessions),
                SessionLengths = BuildSessionLengthBuckets(sessions),
                Streaks = BuildStreaks(sessions),
                Hours = BuildHourStats(sessions),
                WeekdayHours = BuildWeekdayHourHeatmap(sessions)
            };
        }

        /// <summary>Fixed 3-hour bucket boundaries for the weekday x hour heatmap - spec section 13.</summary>
        private static readonly int[] HourBucketStarts = { 0, 3, 6, 9, 12, 15, 18, 21 };

        internal static HourStats BuildHourStats(List<Models.Session> sessions)
        {
            var buckets = new HourBucket[24];
            for (var h = 0; h < 24; h++)
            {
                buckets[h] = new HourBucket { Hour = h };
            }

            foreach (var session in sessions)
            {
                var hour = session.StartedAt.Hour;
                buckets[hour].PlaytimeSeconds += session.DurationSeconds;
                buckets[hour].SessionCount++;
            }

            var maxSeconds = buckets.Max(b => b.PlaytimeSeconds);
            if (maxSeconds > 0)
            {
                foreach (var bucket in buckets)
                {
                    bucket.BarFraction = bucket.PlaytimeSeconds / (double)maxSeconds;
                }
            }

            return new HourStats { Hours = buckets.ToList() };
        }

        internal static WeekdayHourHeatmap BuildWeekdayHourHeatmap(List<Models.Session> sessions)
        {
            // [weekday index Monday=0..Sunday=6][hour-bucket index 0..7]
            var grid = new WeekdayHourCell[7, HourBucketStarts.Length];
            for (var d = 0; d < 7; d++)
            {
                for (var b = 0; b < HourBucketStarts.Length; b++)
                {
                    grid[d, b] = new WeekdayHourCell
                    {
                        DayOfWeek = (DayOfWeek)(((int)DayOfWeek.Monday + d) % 7),
                        BucketStartHour = HourBucketStarts[b],
                        BucketEndHour = b < HourBucketStarts.Length - 1 ? HourBucketStarts[b + 1] : 24
                    };
                }
            }

            foreach (var session in sessions)
            {
                var dayIndex = ((int)session.StartedAt.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                var bucketIndex = session.StartedAt.Hour / 3;
                grid[dayIndex, bucketIndex].PlaytimeSeconds += session.DurationSeconds;
                grid[dayIndex, bucketIndex].SessionCount++;
            }

            var cells = grid.Cast<WeekdayHourCell>().ToList();
            var maxSeconds = cells.Max(c => c.PlaytimeSeconds);
            if (maxSeconds > 0)
            {
                foreach (var cell in cells)
                {
                    cell.Intensity = cell.PlaytimeSeconds / (double)maxSeconds;
                }
            }

            return new WeekdayHourHeatmap { Cells = cells };
        }

        /// <summary>Inclusive-low/exclusive-high boundaries in seconds, and the label shown for each -
        /// spec section 14's fixed bucket set. &lt;30m, 30m-1h, 1-2h, 2-4h, 4h+.</summary>
        private static readonly (long LowerBoundSeconds, string Label)[] SessionLengthBucketBoundaries =
        {
            (0, "< 30m"),
            (30 * 60, "30m-1h"),
            (60 * 60, "1-2h"),
            (2 * 60 * 60, "2-4h"),
            (4 * 60 * 60, "4h+")
        };

        internal static WeekdayStats BuildWeekdayStats(List<Models.Session> sessions)
        {
            var buckets = new WeekdayBucket[7];
            // Monday-first, matching every other weekday-ordered convention in Astra (spec section 23).
            for (var i = 0; i < 7; i++)
            {
                buckets[i] = new WeekdayBucket { DayOfWeek = (DayOfWeek)(((int)DayOfWeek.Monday + i) % 7) };
            }

            foreach (var session in sessions)
            {
                var index = ((int)session.StartedAt.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                buckets[index].PlaytimeSeconds += session.DurationSeconds;
                buckets[index].SessionCount++;
            }

            var maxSeconds = buckets.Max(b => b.PlaytimeSeconds);
            if (maxSeconds > 0)
            {
                foreach (var bucket in buckets)
                {
                    bucket.BarFraction = bucket.PlaytimeSeconds / (double)maxSeconds;
                }
            }

            return new WeekdayStats { Days = buckets.ToList() };
        }

        internal static List<SessionLengthBucket> BuildSessionLengthBuckets(List<Models.Session> sessions)
        {
            var counts = new int[SessionLengthBucketBoundaries.Length];

            foreach (var session in sessions)
            {
                var bucketIndex = SessionLengthBucketBoundaries.Length - 1;
                for (var i = 0; i < SessionLengthBucketBoundaries.Length - 1; i++)
                {
                    if (session.DurationSeconds < SessionLengthBucketBoundaries[i + 1].LowerBoundSeconds)
                    {
                        bucketIndex = i;
                        break;
                    }
                }
                counts[bucketIndex]++;
            }

            var total = sessions.Count;
            var maxCount = counts.Length > 0 ? counts.Max() : 0;
            var result = new List<SessionLengthBucket>();
            for (var i = 0; i < SessionLengthBucketBoundaries.Length; i++)
            {
                result.Add(new SessionLengthBucket
                {
                    Label = SessionLengthBucketBoundaries[i].Label,
                    SessionCount = counts[i],
                    Percentage = total > 0 ? counts[i] * 100.0 / total : 0,
                    BarFraction = maxCount > 0 ? counts[i] / (double)maxCount : 0
                });
            }

            return result;
        }

        internal static StreakStats BuildStreaks(List<Models.Session> sessions)
        {
            var stats = new StreakStats();

            if (sessions.Count == 0)
            {
                return stats;
            }

            var activeDays = sessions.Select(s => s.StartedAt.Date).Distinct().OrderBy(d => d).ToList();
            stats.LongestStreakDays = LongestConsecutiveRun(activeDays);
            stats.CurrentStreakDays = TrailingConsecutiveRun(activeDays);

            var byWeek = sessions
                .GroupBy(s => TrendAggregationService.StartOfWeek(s.StartedAt.Date))
                .Select(g => new { WeekStart = g.Key, TotalSeconds = g.Sum(s => s.DurationSeconds) })
                .OrderByDescending(w => w.TotalSeconds)
                .First();

            stats.MostActiveWeekStart = byWeek.WeekStart;
            stats.MostActiveWeekSeconds = byWeek.TotalSeconds;

            return stats;
        }

        internal static GamingRhythmStats BuildRhythm(List<Models.Session> sessions)
        {
            var stats = new GamingRhythmStats();

            if (sessions.Count == 0)
            {
                return stats;
            }

            var byWeekday = new long[7]; // index 0 = Sunday, matches DayOfWeek enum
            var byMonth = new long[13]; // index 1-12, 0 unused

            foreach (var session in sessions)
            {
                byWeekday[(int)session.StartedAt.DayOfWeek] += session.DurationSeconds;
                byMonth[session.StartedAt.Month] += session.DurationSeconds;
            }

            var busiestWeekdayIndex = Enumerable.Range(0, 7).OrderByDescending(i => byWeekday[i]).First();
            stats.BusiestDayOfWeek = (DayOfWeek)busiestWeekdayIndex;
            stats.BusiestDayOfWeekSeconds = byWeekday[busiestWeekdayIndex];

            var busiestMonthIndex = Enumerable.Range(1, 12).OrderByDescending(i => byMonth[i]).First();
            stats.BusiestMonth = busiestMonthIndex;
            stats.BusiestMonthSeconds = byMonth[busiestMonthIndex];

            var activeDays = sessions.Select(s => s.StartedAt.Date).Distinct().OrderBy(d => d).ToList();
            stats.LongestStreakDays = LongestConsecutiveRun(activeDays);
            stats.CurrentStreakDays = TrailingConsecutiveRun(activeDays);

            return stats;
        }

        internal static SessionStats BuildSessionStats(List<Models.Session> sessions)
        {
            var stats = new SessionStats { TotalSessions = sessions.Count };

            if (sessions.Count == 0)
            {
                return stats;
            }

            stats.AverageSessionSeconds = sessions.Average(s => (double)s.DurationSeconds);
            stats.LongestSessionSeconds = sessions.Max(s => s.DurationSeconds);
            stats.ShortestSessionSeconds = sessions.Min(s => s.DurationSeconds);

            var activeDayCount = sessions.Select(s => s.StartedAt.Date).Distinct().Count();
            stats.AverageSessionsPerActiveDay = activeDayCount > 0 ? (double)sessions.Count / activeDayCount : 0;

            return stats;
        }

        /// <summary>Longest run of calendar days each exactly one day apart, within a sorted, deduplicated
        /// list of active dates.</summary>
        private static int LongestConsecutiveRun(List<DateTime> sortedDistinctDays)
        {
            if (sortedDistinctDays.Count == 0)
            {
                return 0;
            }

            var longest = 1;
            var current = 1;
            for (var i = 1; i < sortedDistinctDays.Count; i++)
            {
                if ((sortedDistinctDays[i] - sortedDistinctDays[i - 1]).Days == 1)
                {
                    current++;
                    longest = Math.Max(longest, current);
                }
                else
                {
                    current = 1;
                }
            }

            return longest;
        }

        /// <summary>Consecutive run of active days ending on the most recent active day in the list -
        /// "current streak" per spec section 15, evaluated within the queried range rather than against
        /// wall-clock "today" so it stays consistent with whatever window the rest of the page is showing.</summary>
        private static int TrailingConsecutiveRun(List<DateTime> sortedDistinctDays)
        {
            if (sortedDistinctDays.Count == 0)
            {
                return 0;
            }

            var streak = 1;
            for (var i = sortedDistinctDays.Count - 1; i > 0; i--)
            {
                if ((sortedDistinctDays[i] - sortedDistinctDays[i - 1]).Days == 1)
                {
                    streak++;
                }
                else
                {
                    break;
                }
            }

            return streak;
        }
    }
}
