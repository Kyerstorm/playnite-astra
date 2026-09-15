using System.Collections.Generic;
using Astra.Services;

namespace Astra.Tests.Fakes
{
    public class FakeGameInfoProvider : IGameInfoProvider
    {
        private readonly List<GameInfo> games = new List<GameInfo>();

        public FakeGameInfoProvider Add(GameInfo game)
        {
            games.Add(game);
            return this;
        }

        public IEnumerable<GameInfo> GetAllGames() => games;
    }
}
