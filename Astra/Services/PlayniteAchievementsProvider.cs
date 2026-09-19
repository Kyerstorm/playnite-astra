using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Astra.Models;

namespace Astra.Services
{
    /// <summary>
    /// Real implementation of IAchievementsProvider, reading directly from PlayniteAchievements'
    /// own SQLite cache. This is the same reverse-engineered-sibling-plugin-storage trick
    /// GameActivityImporter already uses for GameActivity: no dependency on their assembly, no
    /// reflection into their running plugin instance - just their fixed plugin GUID and their
    /// documented-by-inspection on-disk schema (v17 as of PlayniteAchievements 3.2.1).
    ///
    /// Read-only. Astra never writes to this database. Every query is wrapped in a try/catch that
    /// degrades to an empty result rather than throwing, since PlayniteAchievements is actively
    /// developed and could rename/drop a column Astra depends on in a future release.
    /// </summary>
    public class PlayniteAchievementsProvider : IAchievementsProvider
    {
        // Fixed in PlayniteAchievements' own source (PlayniteAchievementsPlugin.cs, override Guid Id).
        private const string PlayniteAchievementsPluginId = "e6aad2c9-6e06-4d8d-ac55-ac3b252b5f7b";

        private readonly string dbPath;

        public PlayniteAchievementsProvider(string extensionsDataRoot)
        {
            dbPath = Path.Combine(extensionsDataRoot, PlayniteAchievementsPluginId, "achievement_cache.db");
        }

        public bool IsAvailable => File.Exists(dbPath);

        public Dictionary<Guid, AchievementProgress> GetProgressForGames(IEnumerable<Guid> gameIds)
        {
            var ids = gameIds?.ToList() ?? new List<Guid>();
            var result = new Dictionary<Guid, AchievementProgress>();

            if (!IsAvailable || ids.Count == 0)
            {
                return result;
            }

            try
            {
                using (var connection = OpenReadOnlyConnection())
                using (var command = connection.CreateCommand())
                {
                    var paramNames = ids.Select((id, index) => "@id" + index).ToList();
                    command.CommandText = $@"
                        SELECT g.PlayniteGameId AS PlayniteGameId,
                               SUM(p.AchievementsUnlocked) AS Unlocked,
                               SUM(p.TotalAchievements) AS Total
                        FROM UserGameProgress p
                        JOIN Games g ON g.Id = p.GameId
                        JOIN Users u ON u.Id = p.UserId
                        WHERE u.IsCurrentUser = 1
                          AND g.PlayniteGameId IN ({string.Join(",", paramNames)})
                        GROUP BY g.PlayniteGameId;";

                    for (var i = 0; i < ids.Count; i++)
                    {
                        command.Parameters.AddWithValue(paramNames[i], ids[i].ToString());
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (Guid.TryParse(reader["PlayniteGameId"] as string, out var gameId))
                            {
                                result[gameId] = new AchievementProgress
                                {
                                    Unlocked = Convert.ToInt32(reader["Unlocked"]),
                                    Total = Convert.ToInt32(reader["Total"])
                                };
                            }
                        }
                    }
                }
            }
            catch (SQLiteException)
            {
                return new Dictionary<Guid, AchievementProgress>();
            }

            return result;
        }

        public List<UnlockedAchievementInfo> GetUnlockedAchievements(DateTime startInclusive, DateTime endExclusive)
        {
            var result = new List<UnlockedAchievementInfo>();

            if (!IsAvailable)
            {
                return result;
            }

            try
            {
                using (var connection = OpenReadOnlyConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT g.PlayniteGameId AS PlayniteGameId,
                               g.GameName AS GameName,
                               d.DisplayName AS AchievementName,
                               d.GlobalPercentUnlocked AS GlobalPercentUnlocked,
                               a.UnlockTimeUtc AS UnlockTimeUtc
                        FROM UserAchievements a
                        JOIN UserGameProgress p ON p.Id = a.UserGameProgressId
                        JOIN Users u ON u.Id = p.UserId
                        JOIN AchievementDefinitions d ON d.Id = a.AchievementDefinitionId
                        JOIN Games g ON g.Id = d.GameId
                        WHERE u.IsCurrentUser = 1
                          AND a.Unlocked = 1
                          AND a.UnlockTimeUtc >= @start
                          AND a.UnlockTimeUtc < @end;";
                    command.Parameters.AddWithValue("@start", startInclusive.ToString("O"));
                    command.Parameters.AddWithValue("@end", endExclusive.ToString("O"));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!Guid.TryParse(reader["PlayniteGameId"] as string, out var gameId))
                            {
                                continue;
                            }

                            if (!DateTime.TryParse(reader["UnlockTimeUtc"] as string, out var unlockTime))
                            {
                                continue;
                            }

                            result.Add(new UnlockedAchievementInfo
                            {
                                GameId = gameId,
                                GameName = reader["GameName"] as string,
                                AchievementName = reader["AchievementName"] as string,
                                GlobalPercentUnlocked = reader["GlobalPercentUnlocked"] == DBNull.Value
                                    ? (double?)null
                                    : Convert.ToDouble(reader["GlobalPercentUnlocked"]),
                                UnlockTimeUtc = unlockTime
                            });
                        }
                    }
                }
            }
            catch (SQLiteException)
            {
                return new List<UnlockedAchievementInfo>();
            }

            return result;
        }

        private SQLiteConnection OpenReadOnlyConnection()
        {
            var connectionString = new SQLiteConnectionStringBuilder
            {
                DataSource = dbPath,
                ReadOnly = true
            }.ToString();

            var connection = new SQLiteConnection(connectionString);
            connection.Open();
            return connection;
        }
    }
}
