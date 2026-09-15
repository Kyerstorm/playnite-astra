using Astra.Models;
using Astra.Services;
using Newtonsoft.Json;
using Xunit;

namespace Astra.Tests
{
    public class RecapExporterTests
    {
        [Fact]
        public void ToJson_RoundTrips_AllRecapFields()
        {
            var recap = new RecapData
            {
                Year = 2026,
                TotalPlaytimeSeconds = 12345,
                TotalSessions = 7,
                ActiveDays = 4
            };
            recap.TopGames.Add(new GameRecapEntry { Name = "Game A", PlaytimeSeconds = 100, SessionCount = 2 });
            recap.NewGamesThisYear.Add(new GameRecapEntry { Name = "Game B" });

            var json = new RecapExporter().ToJson(recap);
            var roundTripped = JsonConvert.DeserializeObject<RecapData>(json);

            Assert.Equal(recap.Year, roundTripped.Year);
            Assert.Equal(recap.TotalPlaytimeSeconds, roundTripped.TotalPlaytimeSeconds);
            Assert.Single(roundTripped.TopGames);
            Assert.Equal("Game A", roundTripped.TopGames[0].Name);
            Assert.Single(roundTripped.NewGamesThisYear);
        }
    }
}
