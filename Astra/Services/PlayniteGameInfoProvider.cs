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
                Added = g.Added,
                // Resolved fresh every call, never cached beyond this — Game.CoverImage can
                // change out from under Astra (e.g. Cover-Swapper rotating covers), and re-reading
                // it live is the only thing that keeps Astra showing whatever cover is currently active.
                CoverImagePath = ResolveCoverPath(g.CoverImage)
            }).ToList();
        }

        private string ResolveCoverPath(string coverImage)
        {
            if (string.IsNullOrEmpty(coverImage))
            {
                return null;
            }

            try
            {
                var path = api.Database.GetFullFilePath(coverImage);
                return !string.IsNullOrEmpty(path) && System.IO.File.Exists(path) ? path : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
