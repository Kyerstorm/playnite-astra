using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

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
            connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
            Initialize();
        }

        private SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(connectionString);
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
        /// Deletes all recorded session history. Does not touch Playnite's own
        /// library/game data — only Astra's local tracking table.
        /// </summary>
        public void ClearAllData()
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM Sessions;";
                command.ExecuteNonQuery();
            }
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
