using System;
using System.Collections.Generic;
using System.Linq;
using Astra.Models;
using Astra.Services;

namespace Astra.Tests.Fakes
{
    public class FakeAchievementsProvider : IAchievementsProvider
    {
        private readonly Dictionary<Guid, AchievementProgress> progress = new Dictionary<Guid, AchievementProgress>();
        private readonly List<UnlockedAchievementInfo> unlocks = new List<UnlockedAchievementInfo>();

        public bool IsAvailable { get; set; } = true;

        public FakeAchievementsProvider WithProgress(Guid gameId, int unlocked, int total)
        {
            progress[gameId] = new AchievementProgress { Unlocked = unlocked, Total = total };
            return this;
        }

        public FakeAchievementsProvider WithUnlock(UnlockedAchievementInfo unlock)
        {
            unlocks.Add(unlock);
            return this;
        }

        public Dictionary<Guid, AchievementProgress> GetProgressForGames(IEnumerable<Guid> gameIds) =>
            gameIds.Where(progress.ContainsKey).ToDictionary(id => id, id => progress[id]);

        public List<UnlockedAchievementInfo> GetUnlockedAchievements(DateTime startInclusive, DateTime endExclusive) =>
            unlocks.Where(u => u.UnlockTimeUtc >= startInclusive && u.UnlockTimeUtc < endExclusive).ToList();
    }
}
