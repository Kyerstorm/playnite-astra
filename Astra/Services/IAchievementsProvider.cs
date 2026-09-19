using System;
using System.Collections.Generic;
using Astra.Models;

namespace Astra.Services
{
    /// <summary>Thin, testable seam over PlayniteAchievements' own SQLite cache
    /// (ExtensionsData\e6aad2c9-6e06-4d8d-ac55-ac3b252b5f7b\achievement_cache.db). Read-only;
    /// Astra never writes to this database. The real implementation degrades to
    /// IsAvailable == false / empty results whenever the file is missing, locked, or its schema
    /// doesn't match what these queries expect - never throws.</summary>
    public interface IAchievementsProvider
    {
        bool IsAvailable { get; }
        Dictionary<Guid, AchievementProgress> GetProgressForGames(IEnumerable<Guid> gameIds);
        List<UnlockedAchievementInfo> GetUnlockedAchievements(DateTime startInclusive, DateTime endExclusive);
    }
}
