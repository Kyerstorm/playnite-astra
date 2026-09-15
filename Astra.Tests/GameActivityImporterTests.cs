using System;
using System.IO;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class GameActivityImporterTests
    {
        private static string CreateGameActivitySourceDir(out string extensionsDataRoot)
        {
            extensionsDataRoot = Path.Combine(Path.GetTempPath(), "AstraTests_ExtData_" + Guid.NewGuid());
            var gameActivityDir = Path.Combine(extensionsDataRoot, GameActivityImporter.GameActivityPluginId, "GameActivity");
            Directory.CreateDirectory(gameActivityDir);
            return gameActivityDir;
        }

        [Fact]
        public void Import_MissingGameActivityInstall_ReportsSourceNotFound()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var importer = new GameActivityImporter(db);
            var emptyRoot = Path.Combine(Path.GetTempPath(), "AstraTests_NoSource_" + Guid.NewGuid());

            var result = importer.Import(emptyRoot);

            Assert.False(result.SourceFound);
            Assert.Equal(0, result.SessionsImported);
        }

        [Fact]
        public void Import_RealGameActivitySchema_ImportsEachSessionAsOneRow()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();
            var sourceDir = CreateGameActivitySourceDir(out var extensionsRoot);

            // Mirrors the real on-disk shape observed in an installed GameActivity
            // copy: SessionPlaytime + Items[].DateSession/ElapsedSeconds. The
            // "Details" hardware-telemetry array is present in real files but
            // intentionally not modeled/consumed here.
            var json = @"{
                ""SessionPlaytime"": 5400,
                ""Items"": [
                    { ""DateSession"": ""2026-03-02T04:42:39.5174234Z"", ""ElapsedSeconds"": 1800, ""Details"": [] },
                    { ""DateSession"": ""2026-03-02T18:51:08.6925877Z"", ""ElapsedSeconds"": 3600, ""Details"": [] }
                ]
            }";
            File.WriteAllText(Path.Combine(sourceDir, gameId + ".json"), json);

            var result = new GameActivityImporter(db).Import(extensionsRoot);

            Assert.True(result.SourceFound);
            Assert.Equal(2, result.SessionsImported);
            Assert.Equal(2, db.GetSessionsForYear(2026).Count);
        }

        [Fact]
        public void Import_RunTwice_IsIdempotentAndSkipsAlreadyImportedSessions()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();
            var sourceDir = CreateGameActivitySourceDir(out var extensionsRoot);
            var json = @"{""SessionPlaytime"":1800,""Items"":[{""DateSession"":""2026-03-02T04:42:39.5174234Z"",""ElapsedSeconds"":1800}]}";
            File.WriteAllText(Path.Combine(sourceDir, gameId + ".json"), json);

            var importer = new GameActivityImporter(db);
            importer.Import(extensionsRoot);
            var secondRun = importer.Import(extensionsRoot);

            Assert.Equal(0, secondRun.SessionsImported);
            Assert.Equal(1, secondRun.SessionsSkippedDuplicate);
            Assert.Equal(1, db.GetSessionCount());
        }

        [Fact]
        public void Import_MalformedJsonFile_IsSkippedAndReportedRatherThanThrowing()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var gameId = Guid.NewGuid();
            var sourceDir = CreateGameActivitySourceDir(out var extensionsRoot);
            File.WriteAllText(Path.Combine(sourceDir, gameId + ".json"), "{ not valid json ");

            var result = new GameActivityImporter(db).Import(extensionsRoot);

            Assert.Single(result.FilesFailedToParse);
            Assert.Equal(0, result.SessionsImported);
        }

        [Fact]
        public void Import_NonGuidFileName_IsIgnored()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var sourceDir = CreateGameActivitySourceDir(out var extensionsRoot);
            File.WriteAllText(Path.Combine(sourceDir, "not-a-guid.json"), "{}");

            var result = new GameActivityImporter(db).Import(extensionsRoot);

            Assert.Equal(0, result.GamesScanned);
        }
    }
}
