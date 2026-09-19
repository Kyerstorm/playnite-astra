using System;

namespace Astra.Models
{
    /// <summary>One unlocked achievement, resolved from PlayniteAchievements' own SQLite cache via
    /// IAchievementsProvider.GetUnlockedAchievements. GlobalPercentUnlocked is nullable because
    /// PlayniteAchievements itself stores it as nullable (not every provider/achievement has a
    /// global rarity figure) - callers must exclude nulls from any "rarest" comparison rather than
    /// treating null as more or less rare than a real value.</summary>
    public class UnlockedAchievementInfo
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public string AchievementName { get; set; }
        public double? GlobalPercentUnlocked { get; set; }
        public DateTime UnlockTimeUtc { get; set; }
    }
}
