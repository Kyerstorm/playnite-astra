using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Plugins;

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
            // Game.PluginId is a raw Guid (confirmed via reflection against the installed
            // Playnite.SDK.dll — unlike Genres/Platforms/Source it is NOT auto-resolved), so it's
            // looked up against the currently installed library plugins to get a friendly name.
            // Guid.Empty means "no plugin owns this game" — a manually added entry.
            var libraryNames = api.Addons.Plugins.OfType<LibraryPlugin>().ToDictionary(p => p.Id, p => p.Name);

            return api.Database.Games.Select(g => new GameInfo
            {
                Id = g.Id,
                Name = g.Name,
                Added = g.Added,
                // Resolved fresh every call, never cached beyond this — Game.CoverImage can
                // change out from under Astra (e.g. Cover-Swapper rotating covers), and re-reading
                // it live is the only thing that keeps Astra showing whatever cover is currently active.
                CoverImagePath = ResolveCoverPath(g.CoverImage),
                // Game.Genres/Game.Platforms are Playnite SDK convenience properties that resolve
                // straight to List<Genre>/List<Platform> (each with a .Name), not just Guid id lists —
                // confirmed via reflection against the installed Playnite.SDK.dll.
                Genres = g.Genres?.Select(x => x.Name).Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>(),
                Platforms = g.Platforms?.Select(x => x.Name).Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>(),
                Library = g.PluginId == Guid.Empty
                    ? "Manually Added"
                    : (libraryNames.TryGetValue(g.PluginId, out var libraryName) ? libraryName : "Unknown Source"),
                IsHidden = g.Hidden,
                // Game.Playtime/Game.PlayCount are both ulong (confirmed via reflection against
                // the installed Playnite.SDK.dll) - clamped into long/int since GameRecapEntry's
                // PlaytimeSeconds/SessionCount are those types and no real game reaches ulong's range.
                NativePlaytimeSeconds = (long)g.Playtime,
                NativePlayCount = (int)Math.Min(g.PlayCount, int.MaxValue)
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
