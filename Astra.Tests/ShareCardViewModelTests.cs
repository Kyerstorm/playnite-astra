using System.Collections.Generic;
using System.Linq;
using Astra.Models;
using Astra.Views;
using Xunit;

namespace Astra.Tests
{
    public class ShareCardViewModelTests
    {
        [Fact]
        public void TopFive_FewerThanFiveGames_ReturnsAllOfThem()
        {
            var recap = new RecapData();
            recap.TopGames.Add(new GameRecapEntry { Name = "A" });
            recap.TopGames.Add(new GameRecapEntry { Name = "B" });
            recap.TopGames.Add(new GameRecapEntry { Name = "C" });

            var card = new ShareCardViewModel { Recap = recap };

            Assert.Equal(new[] { "A", "B", "C" }, card.TopFive.Select(e => e.Name));
        }

        [Fact]
        public void TopFive_MoreThanFiveGames_TruncatesToFive()
        {
            var recap = new RecapData();
            for (var i = 0; i < 8; i++)
            {
                recap.TopGames.Add(new GameRecapEntry { Name = $"Game {i}" });
            }

            var card = new ShareCardViewModel { Recap = recap };

            Assert.Equal(5, card.TopFive.Count);
            Assert.Equal(new[] { "Game 0", "Game 1", "Game 2", "Game 3", "Game 4" }, card.TopFive.Select(e => e.Name));
        }

        [Fact]
        public void TopFive_NullRecap_ReturnsEmptyListNotNull()
        {
            var card = new ShareCardViewModel { Recap = null };

            Assert.NotNull(card.TopFive);
            Assert.Empty(card.TopFive);
        }

        [Fact]
        public void TopFive_EmptyTopGames_ReturnsEmptyList()
        {
            var card = new ShareCardViewModel { Recap = new RecapData() };

            Assert.Empty(card.TopFive);
        }
    }
}
