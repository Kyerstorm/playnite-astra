using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SQLite;

namespace Astra.Data
{
    /// <summary>
    /// Owns the SQLite file that stores Astra's own session history.
    /// Lives under the plugin's user data path, never next to the DLL.
    /// </summary>
    public class AstraDatabase
    {
        private const int SchemaVersion = 1;

        // A fixed, kind-agnostic format so timestamps sort correctly as plain
        // text in SQLite. DateTime.ToString("O") is NOT safe here: it appends
        // a UTC offset only for Local-kind values and nothing for Unspecified,
        // so two timestamps of the same instant can serialize to different
        // lengths and compare incorrectly with a plain string range query.
        // All DateTimes are treated as local wall-clock time and stored/read
        // back verbatim, with no timezone conversion.
        private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff";

        private readonly string connectionString;

        public AstraDatabase(string pluginUserDataPath)
        {
            Directory.CreateDirectory(pluginUserDataPath);
            var dbPath = Path.Combine(pluginUserDataPath, "astra.db");
            connectionString = new SQLiteConnectionStringBuilder { DataSource = dbPath }.ToString();
            Initialize();
        }

        private SQLiteConnection OpenConnection()
        {
            var connection = new SQLiteConnection(connectionString);
            connection.Open();
            return connection;
        }

        private void Initialize()
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Meta (
                        Key TEXT PRIMARY KEY,
                        Value TEXT NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS Sessions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        GameId TEXT NOT NULL,
                        StartedAt TEXT NOT NULL,
                        DurationSeconds INTEGER NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_Sessions_GameId ON Sessions(GameId);
                    CREATE INDEX IF NOT EXISTS IX_Sessions_StartedAt ON Sessions(StartedAt);
                    CREATE TABLE IF NOT EXISTS PlaytimeOverrides (
                        GameId TEXT NOT NULL,
                        Year INTEGER NOT NULL,
                        OverrideSeconds INTEGER NOT NULL,
                        PRIMARY KEY (GameId, Year)
                    );
                    INSERT OR IGNORE INTO Meta (Key, Value) VALUES ('SchemaVersion', @version);
                ";
                command.Parameters.AddWithValue("@version", SchemaVersion.ToString());
                command.ExecuteNonQuery();
            }
        }

        public long InsertSession(Guid gameId, DateTime startedAt, long durationSeconds)
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO Sessions (GameId, StartedAt, DurationSeconds)
                    VALUES (@gameId, @startedAt, @durationSeconds);
                    SELECT last_insert_rowid();
                ";
                command.Parameters.AddWithValue("@gameId", gameId.ToString());
                command.Parameters.AddWithValue("@startedAt", startedAt.ToString(TimestampFormat));
                command.Parameters.AddWithValue("@durationSeconds", durationSeconds);
                return (long)command.ExecuteScalar();
            }
        }

        public List<Models.Session> GetSessionsForYear(int year)
        {
            var results = new List<Models.Session>();
            var yearStart = new DateTime(year, 1, 1).ToString(TimestampFormat);
            var yearEnd = new DateTime(year + 1, 1, 1).ToString(TimestampFormat);

            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT Id, GameId, StartedAt, DurationSeconds
                    FROM Sessions
                    WHERE StartedAt >= @yearStart AND StartedAt < @yearEnd
                    ORDER BY StartedAt ASC;
                ";
                command.Parameters.AddWithValue("@yearStart", yearStart);
                command.Parameters.AddWithValue("@yearEnd", yearEnd);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Models.Session
                        {
                            Id = reader.GetInt64(0),
                            GameId = Guid.Parse(reader.GetString(1)),
                            StartedAt = DateTime.ParseExact(reader.GetString(2), TimestampFormat, System.Globalization.CultureInfo.InvariantCulture),
                            DurationSeconds = reader.GetInt64(3)
                        });
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Bounded range query backing TrendAggregationService - callers pass whatever window
        /// they need (e.g. "last 30 days", "last 10 years") rather than Astra ever loading the
        /// full Sessions table. End is exclusive, matching GetSessionsForYear's convention.
        /// </summary>
        public List<Models.Session> GetSessionsForDateRange(DateTime start, DateTime end)
        {
            var results = new List<Models.Session>();

            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT Id, GameId, StartedAt, DurationSeconds
                    FROM Sessions
                    WHERE StartedAt >= @start AND StartedAt < @end
                    ORDER BY StartedAt ASC;
                ";
                command.Parameters.AddWithValue("@start", start.ToString(TimestampFormat));
                command.Parameters.AddWithValue("@end", end.ToString(TimestampFormat));

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Models.Session
                        {
                            Id = reader.GetInt64(0),
                            GameId = Guid.Parse(reader.GetString(1)),
                            StartedAt = DateTime.ParseExact(reader.GetString(2), TimestampFormat, System.Globalization.CultureInfo.InvariantCulture),
                            DurationSeconds = reader.GetInt64(3)
                        });
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Deletes all recorded session history and manual playtime overrides.
        /// Does not touch Playnite's own library/game data — only Astra's local
        /// tracking tables.
        /// </summary>
        public void ClearAllData()
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM Sessions; DELETE FROM PlaytimeOverrides;";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Sets (or replaces) a manual correction for a game's total tracked
        /// playtime in a given year. RecapAggregator applies this on top of
        /// the computed sum of that game's sessions for the year.
        /// </summary>
        public void SetPlaytimeOverride(Guid gameId, int year, long overrideSeconds)
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT OR REPLACE INTO PlaytimeOverrides (GameId, Year, OverrideSeconds)
                    VALUES (@gameId, @year, @overrideSeconds);
                ";
                command.Parameters.AddWithValue("@gameId", gameId.ToString());
                command.Parameters.AddWithValue("@year", year);
                command.Parameters.AddWithValue("@overrideSeconds", overrideSeconds);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>Removes a manual playtime correction, reverting that game/year to its computed value.</summary>
        public void ClearPlaytimeOverride(Guid gameId, int year)
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM PlaytimeOverrides WHERE GameId = @gameId AND Year = @year;";
                command.Parameters.AddWithValue("@gameId", gameId.ToString());
                command.Parameters.AddWithValue("@year", year);
                command.ExecuteNonQuery();
            }
        }

        public Dictionary<Guid, long> GetPlaytimeOverridesForYear(int year)
        {
            var results = new Dictionary<Guid, long>();

            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT GameId, OverrideSeconds FROM PlaytimeOverrides WHERE Year = @year;";
                command.Parameters.AddWithValue("@year", year);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results[Guid.Parse(reader.GetString(0))] = reader.GetInt64(1);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Used by importers to stay idempotent: a session is considered the
        /// same one if it starts at the same instant for the same game.
        /// </summary>
        public bool HasSession(Guid gameId, DateTime startedAt)
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COUNT(*) FROM Sessions
                    WHERE GameId = @gameId AND StartedAt = @startedAt;
                ";
                command.Parameters.AddWithValue("@gameId", gameId.ToString());
                command.Parameters.AddWithValue("@startedAt", startedAt.ToString(TimestampFormat));
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        /// <summary>Backs Trends' "All years" range preset - a single scalar query, not a full-table load.</summary>
        public int? GetEarliestSessionYear()
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT MIN(StartedAt) FROM Sessions;";
                var result = command.ExecuteScalar();
                if (result == null || result is DBNull)
                {
                    return null;
                }

                return DateTime.ParseExact((string)result, TimestampFormat, System.Globalization.CultureInfo.InvariantCulture).Year;
            }
        }

        public int GetSessionCount()
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM Sessions;";
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }
    }
}
