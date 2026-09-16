using System;
using Astra.Models;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class GamingYearSummaryBuilderTests
    {
        private static TrendAnalyticsResult MakeAnalytics(
            int activeDays = 0, DayOfWeek? busiestDay = null, int? busiestMonth = null, long busiestMonthSeconds = 0,
            int longestStreak = 0, int totalSessions = 0, double avgSessionSeconds = 0, long longestSessionSeconds = 0)
        {
            return new TrendAnalyticsResult
            {
                Sessions = new SessionStats
                {
                    ActiveDays = activeDays,
                    TotalSessions = totalSessions,
                    AverageSessionSeconds = avgSessionSeconds,
                    LongestSessionSeconds = longestSessionSeconds
                },
                Rhythm = new GamingRhythmStats
                {
                    BusiestDayOfWeek = busiestDay,
                    BusiestMonth = busiestMonth,
                    BusiestMonthSeconds = busiestMonthSeconds,
                    LongestStreakDays = longestStreak
                }
            };
        }

        [Fact]
        public void Build_SingularGrammar_ForOneDayOneGame()
        {
            var analytics = MakeAnalytics(activeDays: 1);
            var concentration = new PlaytimeConcentrationStats { TotalGamesTouched = 1 };

            var sentences = GamingYearSummaryBuilder.Build(analytics, concentration);

            Assert.Equal("You played on 1 day across 1 game.", sentences[0]);
        }

        [Fact]
        public void Build_PluralGrammar_ForManyDaysAndGames()
        {
            var analytics = MakeAnalytics(activeDays: 246);
            var concentration = new PlaytimeConcentrationStats { TotalGamesTouched = 83 };

            var sentences = GamingYearSummaryBuilder.Build(analytics, concentration);

            Assert.Equal("You played on 246 days across 83 games.", sentences[0]);
        }

        [Fact]
        public void Build_ZeroGrammar_ForNoActivity()
        {
            var analytics = MakeAnalytics(activeDays: 0);
            var concentration = new PlaytimeConcentrationStats { TotalGamesTouched = 0 };

            var sentences = GamingYearSummaryBuilder.Build(analytics, concentration);

            Assert.Equal("You played on 0 days across 0 games.", sentences[0]);
        }

        [Fact]
        public void Build_BusiestMonthSentence_MatchesSpecExampleFormat()
        {
            var analytics = MakeAnalytics(busiestMonth: 9, busiestMonthSeconds: 42 * 3600 + 42 * 60);
            var concentration = new PlaytimeConcentrationStats();

            var sentences = GamingYearSummaryBuilder.Build(analytics, concentration);

            Assert.Contains("Your busiest month was September with 42h 42m.", sentences);
        }

        [Fact]
        public void Build_NoBusiestMonth_OmitsThatSentenceWithoutThrowing()
        {
            var analytics = MakeAnalytics(busiestMonth: null);
            var concentration = new PlaytimeConcentrationStats();

            var sentences = GamingYearSummaryBuilder.Build(analytics, concentration);

            Assert.DoesNotContain(sentences, s => s.Contains("busiest month"));
        }

        [Fact]
        public void Build_LongestStreakSentence_SingularForOneDay()
        {
            var analytics = MakeAnalytics(longestStreak: 1);
            var concentration = new PlaytimeConcentrationStats();

            var sentences = GamingYearSummaryBuilder.Build(analytics, concentration);

            Assert.Contains("Your longest gaming streak was 1 day.", sentences);
        }

        [Fact]
        public void Build_ZeroStreak_OmitsStreakSentence()
        {
            var analytics = MakeAnalytics(longestStreak: 0);
            var concentration = new PlaytimeConcentrationStats();

            var sentences = GamingYearSummaryBuilder.Build(analytics, concentration);

            Assert.DoesNotContain(sentences, s => s.Contains("streak"));
        }

        [Fact]
        public void Build_SessionSentences_MatchSpecExampleFormat()
        {
            var analytics = MakeAnalytics(totalSessions: 1302, avgSessionSeconds: 3600 + 22 * 60, longestSessionSeconds: 7 * 3600 + 48 * 60);
            var concentration = new PlaytimeConcentrationStats();

            var sentences = GamingYearSummaryBuilder.Build(analytics, concentration);

            Assert.Contains("Your average session lasted 1h 22m.", sentences);
            Assert.Contains("Your longest session lasted 7h 48m.", sentences);
        }

        [Fact]
        public void Build_NoSessions_OmitsSessionSentences()
        {
            var analytics = MakeAnalytics(totalSessions: 0);
            var concentration = new PlaytimeConcentrationStats();

            var sentences = GamingYearSummaryBuilder.Build(analytics, concentration);

            Assert.DoesNotContain(sentences, s => s.Contains("session"));
        }

        [Fact]
        public void Build_NullAnalytics_ReturnsEmptyListNotThrow()
        {
            var sentences = GamingYearSummaryBuilder.Build(null, null);

            Assert.Empty(sentences);
        }

        [Fact]
        public void Build_NeverContainsSubjectiveLanguage()
        {
            var analytics = MakeAnalytics(activeDays: 246, busiestMonth: 9, busiestMonthSeconds: 1000, longestStreak: 11, totalSessions: 1302, avgSessionSeconds: 100, longestSessionSeconds: 200);
            var concentration = new PlaytimeConcentrationStats { TotalGamesTouched = 83 };

            var sentences = GamingYearSummaryBuilder.Build(analytics, concentration);

            var bannedWords = new[] { "amazing", "too much", "great", "impressive", "should", "unhealthy" };
            foreach (var sentence in sentences)
            {
                foreach (var banned in bannedWords)
                {
                    Assert.DoesNotContain(banned, sentence, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }
}
