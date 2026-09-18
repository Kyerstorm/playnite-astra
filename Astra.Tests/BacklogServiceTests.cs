using System;
using System.Collections.Generic;
using Astra.Models;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class BacklogServiceTests
    {
        private static GameInfo Game(string name, bool hidden = false, List<string> genres = null, List<string> platforms = null)
        {
            return new GameInfo
            {
                Id = Guid.NewGuid(),
                Name = name,
                IsHidden = hidden,
                Genres = genres ?? new List<string>(),
                Platforms = platforms ?? new List<string>()
            };
        }

        [Fact]
        public void BuildBacklog_HiddenGame_Excluded()
        {
            var hidden = Game("Hidden Game", hidden: true);
            var games = new List<GameInfo> { hidden };

            var result = BacklogService.BuildBacklog(games, new HashSet<Guid>());

            Assert.Empty(result);
        }

        [Fact]
        public void BuildBacklog_PlayedGame_Excluded()
        {
            var played = Game("Played Game");
            var games = new List<GameInfo> { played };

            var result = BacklogService.BuildBacklog(games, new HashSet<Guid> { played.Id });

            Assert.Empty(result);
        }

        [Fact]
        public void BuildBacklog_UnplayedNonHiddenGame_Included()
        {
            var game = Game("Unplayed Game");
            var games = new List<GameInfo> { game };

            var result = BacklogService.BuildBacklog(games, new HashSet<Guid>());

            Assert.Single(result);
            Assert.Equal(game.Id, result[0].GameId);
            Assert.Equal("Unplayed Game", result[0].Name);
        }

        [Fact]
        public void BuildBacklog_ResultIsAlphabeticalByName()
        {
            var games = new List<GameInfo> { Game("Zelda"), Game("Alan Wake"), Game("mario") };

            var result = BacklogService.BuildBacklog(games, new HashSet<Guid>());

            Assert.Equal(new[] { "Alan Wake", "mario", "Zelda" }, result.ConvertAll(e => e.Name));
        }

        [Fact]
        public void ApplyFilters_NoFiltersSelected_ReturnsAllEntries()
        {
            var entries = new List<BacklogEntry>
            {
                new BacklogEntry { Name = "A" },
                new BacklogEntry { Name = "B" }
            };

            var result = BacklogService.ApplyFilters(entries, new List<string>(), new List<string>());

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void ApplyFilters_GenreSelected_OnlyMatchingGenreEntriesReturned()
        {
            var entries = new List<BacklogEntry>
            {
                new BacklogEntry { Name = "RPG Game", Genres = new List<string> { "RPG" } },
                new BacklogEntry { Name = "Sim Game", Genres = new List<string> { "Simulation" } }
            };

            var result = BacklogService.ApplyFilters(entries, new List<string> { "RPG" }, new List<string>());

            Assert.Single(result);
            Assert.Equal("RPG Game", result[0].Name);
        }

        [Fact]
        public void ApplyFilters_PlatformSelected_OnlyMatchingPlatformEntriesReturned()
        {
            var entries = new List<BacklogEntry>
            {
                new BacklogEntry { Name = "PC Game", Platforms = new List<string> { "PC" } },
                new BacklogEntry { Name = "Switch Game", Platforms = new List<string> { "Switch" } }
            };

            var result = BacklogService.ApplyFilters(entries, new List<string>(), new List<string> { "Switch" });

            Assert.Single(result);
            Assert.Equal("Switch Game", result[0].Name);
        }

        [Fact]
        public void ApplyFilters_BothCategoriesSelected_RequiresBothToMatch()
        {
            var matchesGenreOnly = new BacklogEntry
            {
                Name = "Genre Only",
                Genres = new List<string> { "RPG" },
                Platforms = new List<string> { "PC" }
            };
            var matchesBoth = new BacklogEntry
            {
                Name = "Both",
                Genres = new List<string> { "RPG" },
                Platforms = new List<string> { "Switch" }
            };
            var entries = new List<BacklogEntry> { matchesGenreOnly, matchesBoth };

            var result = BacklogService.ApplyFilters(entries, new List<string> { "RPG" }, new List<string> { "Switch" });

            Assert.Single(result);
            Assert.Equal("Both", result[0].Name);
        }

        [Fact]
        public void ApplyFilters_MultipleGenresSelected_MatchesAnyOfThem()
        {
            var entries = new List<BacklogEntry>
            {
                new BacklogEntry { Name = "RPG", Genres = new List<string> { "RPG" } },
                new BacklogEntry { Name = "Sim", Genres = new List<string> { "Simulation" } },
                new BacklogEntry { Name = "Puzzle", Genres = new List<string> { "Puzzle" } }
            };

            var result = BacklogService.ApplyFilters(entries, new List<string> { "RPG", "Simulation" }, new List<string>());

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void DistinctGenres_ReturnsOnlyGenresPresentInBacklogEntries_SortedAlphabetically()
        {
            var entries = new List<BacklogEntry>
            {
                new BacklogEntry { Name = "A", Genres = new List<string> { "rpg", "Strategy" } },
                new BacklogEntry { Name = "B", Genres = new List<string> { "RPG" } }
            };

            var result = BacklogService.DistinctGenres(entries);

            Assert.Equal(2, result.Count);
            Assert.Equal("Strategy", result[1]);
        }
    }
}
