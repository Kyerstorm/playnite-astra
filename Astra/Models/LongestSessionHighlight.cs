using System;

namespace Astra.Models
{
    /// <summary>The single longest tracked session in a given year, or null if there were none.</summary>
    public class LongestSessionHighlight
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public DateTime StartedAt { get; set; }
        public long DurationSeconds { get; set; }
    }
}
