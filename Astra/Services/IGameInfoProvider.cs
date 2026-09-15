using System;
using System.Collections.Generic;

namespace Astra.Services
{
    public class GameInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime? Added { get; set; }

        /// <summary>Resolved local file path to the game's current cover, or null if unset/unresolvable.</summary>
        public string CoverImagePath { get; set; }
    }

    /// <summary>
    /// Thin seam over Playnite's game database so RecapAggregator can be unit
    /// tested without a real IPlayniteAPI. The real implementation wraps
    /// IPlayniteAPI.Database.Games.
    /// </summary>
    public interface IGameInfoProvider
    {
        IEnumerable<GameInfo> GetAllGames();
    }
}
