using System.Collections.Generic;
using System.Linq;
using Astra.Models;

namespace Astra.Services
{
    /// <summary>Pure, DB/Playnite-free selection logic over already-fetched achievement unlock
    /// lists - same shape as SessionCadenceService/BacklogBurndownService: static class, plain
    /// model in/out, fully unit-testable without SQLite. The one I/O round-trip
    /// (IAchievementsProvider.GetUnlockedAchievements) happens in the caller
    /// (HomeViewModel.Refresh / RecapAggregator.BuildRecap), never here.</summary>
    public static class AchievementHighlightsService
    {
        public static RecentUnlocksHighlight FindRecentUnlocks(List<UnlockedAchievementInfo> unlocksInWindow)
        {
            if (unlocksInWindow == null || unlocksInWindow.Count == 0)
            {
                return null;
            }

            return new RecentUnlocksHighlight { Count = unlocksInWindow.Count };
        }

        /// <summary>Lowest GlobalPercentUnlocked among the given unlocks. Achievements with no
        /// rated GlobalPercentUnlocked (null) are excluded from consideration entirely - never
        /// treated as "rarer" or "more common" than a real value.</summary>
        public static RarestUnlockHighlight FindRarestEver(List<UnlockedAchievementInfo> allTimeUnlocks)
        {
            var rated = allTimeUnlocks?.Where(u => u.GlobalPercentUnlocked.HasValue).ToList();
            if (rated == null || rated.Count == 0)
            {
                return null;
            }

            var rarest = rated.OrderBy(u => u.GlobalPercentUnlocked.Value).First();
            return new RarestUnlockHighlight
            {
                GameName = rarest.GameName,
                AchievementName = rarest.AchievementName,
                GlobalPercentUnlocked = rarest.GlobalPercentUnlocked.Value
            };
        }
    }
}
