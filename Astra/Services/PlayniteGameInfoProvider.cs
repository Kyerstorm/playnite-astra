using System.Collections.Generic;
using System.Linq;
using Playnite.SDK;

namespace Astra.Services
{
    /// <summary>Real implementation of IGameInfoProvider, backed by Playnite's own library.</summary>
    public class PlayniteGameInfoProvider : IGameInfoProvider
    {
        private readonly IPlayniteAPI api;

        public PlayniteGameInfoProvider(IPlayniteAPI api)
        {
            this.api = api;
        }

        public IEnumerable<GameInfo> GetAllGames()
        {
            return api.Database.Games.Select(g => new GameInfo
            {
                Id = g.Id,
                Name = g.Name,
                Added = g.Added
            }).ToList();
        }
    }
}
