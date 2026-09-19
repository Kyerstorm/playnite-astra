using System;
using System.Collections.Generic;
using Astra.Models;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class AchievementHighlightsServiceTests
    {
        [Fact]
        public void FindRecentUnlocks_EmptyList_ReturnsNull()
        {
            Assert.Null(AchievementHighlightsService.FindRecentUnlocks(new List<UnlockedAchievementInfo>()));
        }

        [Fact]
        public void FindRecentUnlocks_NonEmpty_ReturnsCorrectCount()
        {
            var unlocks = new List<UnlockedAchievementInfo>
            {
                new UnlockedAchievementInfo { AchievementName = "A" },
                new UnlockedAchievementInfo { AchievementName = "B" }
            };

            var result = AchievementHighlightsService.FindRecentUnlocks(unlocks);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void FindRarestEver_NullOrEmpty_ReturnsNull()
        {
            Assert.Null(AchievementHighlightsService.FindRarestEver(null));
            Assert.Null(AchievementHighlightsService.FindRarestEver(new List<UnlockedAchievementInfo>()));
        }

        [Fact]
        public void FindRarestEver_ExcludesUnratedAchievements()
        {
            var unlocks = new List<UnlockedAchievementInfo>
            {
                new UnlockedAchievementInfo { AchievementName = "Unrated", GlobalPercentUnlocked = null },
                new UnlockedAchievementInfo { AchievementName = "Rated", GlobalPercentUnlocked = 5.0 }
            };

            var result = AchievementHighlightsService.FindRarestEver(unlocks);

            Assert.Equal("Rated", result.AchievementName);
        }

        [Fact]
        public void FindRarestEver_PicksLowestGlobalPercent()
        {
            var unlocks = new List<UnlockedAchievementInfo>
            {
                new UnlockedAchievementInfo { AchievementName = "Common", GlobalPercentUnlocked = 80.0 },
                new UnlockedAchievementInfo { AchievementName = "Rarest", GlobalPercentUnlocked = 0.5 },
                new UnlockedAchievementInfo { AchievementName = "Uncommon", GlobalPercentUnlocked = 25.0 }
            };

            var result = AchievementHighlightsService.FindRarestEver(unlocks);

            Assert.Equal("Rarest", result.AchievementName);
            Assert.Equal(0.5, result.GlobalPercentUnlocked);
        }
    }
}
