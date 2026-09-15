using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Astra.Data;
using Astra.Models;
using Newtonsoft.Json;

namespace Astra.Services
{
    public class GameActivityImportResult
    {
        public bool SourceFound { get; set; }
        public int GamesScanned { get; set; }
        public int SessionsImported { get; set; }
        public int SessionsSkippedDuplicate { get; set; }
        public List<string> FilesFailedToParse { get; set; } = new List<string>();
    }

    /// <summary>
    /// One-time, user-triggered import of existing play history from Lacro59's
    /// GameActivity plugin, so switching to Astra doesn't lose prior tracking.
    /// Best-effort: GameActivity's on-disk format is undocumented (reverse
    /// engineered from an installed v3.5 copy) and could change in a future
    /// GameActivity release — see BUGS.md. Never runs automatically.
    /// </summary>
    public class GameActivityImporter
    {
        // GameActivity's plugin GUID is fixed in its own source code (Plugin.Id),
        // so this folder name is the same for every Playnite install, not specific
        // to this machine.
        public const string GameActivityPluginId = "afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4";

        private readonly AstraDatabase database;

        public GameActivityImporter(AstraDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <param name="extensionsDataRoot">
        /// Playnite's ExtensionsData directory (the parent of Astra's own
        /// GetPluginUserDataPath() folder).
        /// </param>
        public GameActivityImportResult Import(string extensionsDataRoot)
        {
            var result = new GameActivityImportResult();
            var sourceDir = Path.Combine(extensionsDataRoot, GameActivityPluginId, "GameActivity");

            if (!Directory.Exists(sourceDir))
            {
                result.SourceFound = false;
                return result;
            }

            result.SourceFound = true;

            foreach (var file in Directory.EnumerateFiles(sourceDir, "*.json"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (!Guid.TryParse(fileName, out var gameId))
                {
                    continue;
                }

                result.GamesScanned++;

                GameActivityFile parsed;
                try
                {
                    var json = File.ReadAllText(file);
                    parsed = JsonConvert.DeserializeObject<GameActivityFile>(json);
                }
                catch (JsonException)
                {
                    result.FilesFailedToParse.Add(file);
                    continue;
                }

                if (parsed?.Items == null)
                {
                    continue;
                }

                foreach (var item in parsed.Items)
                {
                    if (item.ElapsedSeconds <= 0)
                    {
                        continue;
                    }

                    var startedAt = item.DateSession.ToLocalTime();
                    if (database.HasSession(gameId, startedAt))
                    {
                        result.SessionsSkippedDuplicate++;
                        continue;
                    }

                    database.InsertSession(gameId, startedAt, item.ElapsedSeconds);
                    result.SessionsImported++;
                }
            }

            return result;
        }
    }
}
