using System.Collections.Generic;
using System.Globalization;
using Astra.Models;

namespace Astra.Services
{
    /// <summary>
    /// Turns an already-computed TrendAnalyticsResult into plain factual sentences - "Your Gaming
    /// Year" (spec section 19). Deliberately not AI-generated: every sentence is a fixed template
    /// filled from a number that's already been calculated elsewhere, so it's exactly as
    /// deterministic and testable as the numbers themselves. No subjective, motivational, or
    /// health/lifestyle language - ever.
    /// </summary>
    public static class GamingYearSummaryBuilder
    {
        public static List<string> Build(TrendAnalyticsResult analytics, PlaytimeConcentrationStats concentration)
        {
            var sentences = new List<string>();

            if (analytics == null)
            {
                return sentences;
            }

            var activeDays = analytics.Sessions?.ActiveDays ?? 0;
            var gamesTouched = concentration?.TotalGamesTouched ?? 0;
            sentences.Add($"You played on {Pluralize(activeDays, "day")} across {Pluralize(gamesTouched, "game")}.");

            if (analytics.Rhythm?.BusiestMonth != null)
            {
                var monthName = new System.DateTime(2000, analytics.Rhythm.BusiestMonth.Value, 1).ToString("MMMM", CultureInfo.InvariantCulture);
                sentences.Add($"Your busiest month was {monthName} with {FormatPlaytime(analytics.Rhythm.BusiestMonthSeconds)}.");
            }

            if (analytics.Rhythm != null && analytics.Rhythm.LongestStreakDays > 0)
            {
                sentences.Add($"Your longest gaming streak was {Pluralize(analytics.Rhythm.LongestStreakDays, "day")}.");
            }

            if (analytics.Sessions != null && analytics.Sessions.TotalSessions > 0)
            {
                sentences.Add($"Your average session lasted {FormatPlaytime((long)analytics.Sessions.AverageSessionSeconds)}.");
                sentences.Add($"Your longest session lasted {FormatPlaytime(analytics.Sessions.LongestSessionSeconds)}.");
            }

            return sentences;
        }

        private static string Pluralize(int count, string noun)
        {
            return count == 1 ? $"1 {noun}" : $"{count} {noun}s";
        }

        private static string FormatPlaytime(long seconds)
        {
            var hours = seconds / 3600;
            var minutes = (seconds % 3600) / 60;
            if (hours <= 0)
            {
                return minutes > 0 ? $"{minutes}m" : "0m";
            }
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }
    }
}
