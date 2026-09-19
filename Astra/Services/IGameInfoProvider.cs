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

        /// <summary>Resolved genre/platform names, never null (empty list when the game has none set).</summary>
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Platforms { get; set; } = new List<string>();

        /// <summary>Name of the Playnite library plugin that manages this game (Steam, GOG, Epic, Xbox, ...),
        /// resolved from Game.PluginId, or "Manually Added" when PluginId is empty (no plugin owns it). Unlike
        /// Game.Source (a free-form, often-unset storefront tag), every game resolves to some Library value,
        /// which is why this replaced a by-Source breakdown that left most manually-imported games uncounted.</summary>
        public string Library { get; set; }

        /// <summary>Playnite's own user-set "hidden from library" flag. Used by BacklogService to
        /// exclude hidden games from the Backlog page - unrelated to any Astra session data.</summary>
        public bool IsHidden { get; set; }

        /// <summary>Playnite's own cumulative lifetime playtime for this game (Game.Playtime, in
        /// seconds), tracked natively by Playnite regardless of whether Astra has ever recorded a
        /// session for it - covers play that happened before Astra was installed or through a
        /// library Astra has no tracked sessions for. Used only by RecapAggregator.BuildAllTimeRecap
        /// to fill in games/libraries Astra's own Sessions table has no record of; yearly BuildRecap
        /// is untouched since Playnite exposes no per-year breakdown of this figure.</summary>
        public long NativePlaytimeSeconds { get; set; }

        /// <summary>Playnite's own cumulative lifetime launch count (Game.PlayCount) - the
        /// All-Time-only analog of NativePlaytimeSeconds for GameRecapEntry.SessionCount.</summary>
        public int NativePlayCount { get; set; }
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
