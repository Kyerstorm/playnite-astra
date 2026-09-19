using System;
using System.Data.SQLite;
using System.IO;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class PlayniteAchievementsProviderTests
    {
        private const string PluginId = "e6aad2c9-6e06-4d8d-ac55-ac3b252b5f7b";

        private static string CreateTempRoot()
        {
            var dir = Path.Combine(Path.GetTempPath(), "AstraTests_Achievements_" + Guid.NewGuid());
            Directory.CreateDirectory(Path.Combine(dir, PluginId));
            return dir;
        }

        private static string DbPathFor(string root) => Path.Combine(root, PluginId, "achievement_cache.db");

        /// <summary>Builds a minimal fixture database mirroring PlayniteAchievements' real schema
        /// (v17, reverse-engineered from its public source this session), trimmed to only the
        /// columns PlayniteAchievementsProvider reads/joins on.</summary>
        private static void CreateFixtureDatabase(string root)
        {
            var dbPath = DbPathFor(root);
            var connectionString = new SQLiteConnectionStringBuilder { DataSource = dbPath }.ToString();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE Users (Id INTEGER PRIMARY KEY, IsCurrentUser INTEGER NOT NULL);
                        CREATE TABLE Games (Id INTEGER PRIMARY KEY, PlayniteGameId TEXT, GameName TEXT);
                        CREATE TABLE UserGameProgress (
                            Id INTEGER PRIMARY KEY, UserId INTEGER NOT NULL, GameId INTEGER NOT NULL,
                            AchievementsUnlocked INTEGER NOT NULL, TotalAchievements INTEGER NOT NULL);
                        CREATE TABLE AchievementDefinitions (
                            Id INTEGER PRIMARY KEY, GameId INTEGER NOT NULL, DisplayName TEXT,
                            GlobalPercentUnlocked REAL NULL);
                        CREATE TABLE UserAchievements (
                            Id INTEGER PRIMARY KEY, UserGameProgressId INTEGER NOT NULL,
                            AchievementDefinitionId INTEGER NOT NULL, Unlocked INTEGER NOT NULL,
                            UnlockTimeUtc TEXT NULL);";
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void Exec(string root, string sql)
        {
            var connectionString = new SQLiteConnectionStringBuilder { DataSource = DbPathFor(root) }.ToString();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        [Fact]
        public void IsAvailable_FileMissing_ReturnsFalse()
        {
            var root = CreateTempRoot();
            var provider = new PlayniteAchievementsProvider(root);

            Assert.False(provider.IsAvailable);
        }

        [Fact]
        public void GetProgressForGames_FiltersToCurrentUser_IgnoresOtherUsers()
        {
            var root = CreateTempRoot();
            CreateFixtureDatabase(root);
            var gameId = Guid.NewGuid();

            Exec(root, "INSERT INTO Users (Id, IsCurrentUser) VALUES (1, 1), (2, 0);");
            Exec(root, $"INSERT INTO Games (Id, PlayniteGameId, GameName) VALUES (1, '{gameId}', 'Game A');");
            Exec(root, "INSERT INTO UserGameProgress (Id, UserId, GameId, AchievementsUnlocked, TotalAchievements) VALUES " +
                        "(1, 1, 1, 10, 20), " + // current user
                        "(2, 2, 1, 999, 999);"); // friend - must be ignored

            var provider = new PlayniteAchievementsProvider(root);
            var result = provider.GetProgressForGames(new[] { gameId });

            Assert.Equal(10, result[gameId].Unlocked);
            Assert.Equal(20, result[gameId].Total);
        }

        [Fact]
        public void GetProgressForGames_GameNotInDatabase_ReturnsNoEntry()
        {
            var root = CreateTempRoot();
            CreateFixtureDatabase(root);
            var requestedId = Guid.NewGuid();

            var provider = new PlayniteAchievementsProvider(root);
            var result = provider.GetProgressForGames(new[] { requestedId });

            Assert.False(result.ContainsKey(requestedId));
        }

        [Fact]
        public void GetUnlockedAchievements_FiltersByDateRange_ExcludesOutOfRangeUnlocks()
        {
            var root = CreateTempRoot();
            CreateFixtureDatabase(root);
            var gameId = Guid.NewGuid();

            Exec(root, "INSERT INTO Users (Id, IsCurrentUser) VALUES (1, 1);");
            Exec(root, $"INSERT INTO Games (Id, PlayniteGameId, GameName) VALUES (1, '{gameId}', 'Game A');");
            Exec(root, "INSERT INTO UserGameProgress (Id, UserId, GameId, AchievementsUnlocked, TotalAchievements) VALUES (1, 1, 1, 1, 5);");
            Exec(root, "INSERT INTO AchievementDefinitions (Id, GameId, DisplayName, GlobalPercentUnlocked) VALUES " +
                        "(1, 1, 'In Range', 10.0), (2, 1, 'Out Of Range', 20.0);");
            Exec(root, "INSERT INTO UserAchievements (Id, UserGameProgressId, AchievementDefinitionId, Unlocked, UnlockTimeUtc) VALUES " +
                        "(1, 1, 1, 1, '2026-06-15T00:00:00.0000000'), " +
                        "(2, 1, 2, 1, '2020-01-01T00:00:00.0000000');");

            var provider = new PlayniteAchievementsProvider(root);
            var result = provider.GetUnlockedAchievements(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));

            Assert.Single(result);
            Assert.Equal("In Range", result[0].AchievementName);
        }

        [Fact]
        public void GetUnlockedAchievements_ExcludesUnlockedFalseRows()
        {
            var root = CreateTempRoot();
            CreateFixtureDatabase(root);
            var gameId = Guid.NewGuid();

            Exec(root, "INSERT INTO Users (Id, IsCurrentUser) VALUES (1, 1);");
            Exec(root, $"INSERT INTO Games (Id, PlayniteGameId, GameName) VALUES (1, '{gameId}', 'Game A');");
            Exec(root, "INSERT INTO UserGameProgress (Id, UserId, GameId, AchievementsUnlocked, TotalAchievements) VALUES (1, 1, 1, 0, 5);");
            Exec(root, "INSERT INTO AchievementDefinitions (Id, GameId, DisplayName, GlobalPercentUnlocked) VALUES (1, 1, 'Still Locked', 10.0);");
            Exec(root, "INSERT INTO UserAchievements (Id, UserGameProgressId, AchievementDefinitionId, Unlocked, UnlockTimeUtc) VALUES " +
                        "(1, 1, 1, 0, '2026-06-15T00:00:00.0000000');");

            var provider = new PlayniteAchievementsProvider(root);
            var result = provider.GetUnlockedAchievements(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));

            Assert.Empty(result);
        }
    }
}
