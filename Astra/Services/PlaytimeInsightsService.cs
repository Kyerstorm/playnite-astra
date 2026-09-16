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
                Sessions = BuildSessionStats(sessions)
            };
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
