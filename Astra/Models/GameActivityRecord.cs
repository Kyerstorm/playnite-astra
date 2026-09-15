using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Astra.Models
{
    /// <summary>
    /// Mirrors the on-disk schema of Lacro59's GameActivity plugin
    /// (one JSON file per game, named &lt;gameId&gt;.json, under
    /// ExtensionsData\afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4\GameActivity\).
    /// Only the fields Astra needs (session start + duration) are mapped;
    /// GameActivity's per-session hardware telemetry ("Details") is ignored.
    /// This schema is undocumented and reverse-engineered from an installed
    /// copy (v3.5) — a future GameActivity release could change it, see BUGS.md.
    /// </summary>
    public class GameActivityFile
    {
        public long SessionPlaytime { get; set; }
        public List<GameActivityItem> Items { get; set; } = new List<GameActivityItem>();
    }

    public class GameActivityItem
    {
        public DateTime DateSession { get; set; }
        public long ElapsedSeconds { get; set; }
    }
}
