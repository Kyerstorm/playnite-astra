using System;
using System.Collections.Generic;

namespace Astra.Models
{
    /// <summary>One never-played, non-hidden library game shown on the Backlog page.</summary>
    public class BacklogEntry
    {
        public Guid GameId { get; set; }
        public string Name { get; set; }
        public DateTime? Added { get; set; }

        /// <summary>Resolved local cover file path, or null if the game has none / it's unresolvable.</summary>
        public string CoverImagePath { get; set; }

        /// <summary>Deterministic placeholder shown in place of a cover when CoverImagePath is null.</summary>
        public string PlaceholderColorHex { get; set; }
        public string PlaceholderInitials { get; set; }

        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Platforms { get; set; } = new List<string>();

        /// <summary>Formatted here (not in XAML/StringFormat) - same "format text in code" convention as
        /// GamingYearSummaryBuilder/HomeViewModel.BacklogDeltaText.</summary>
        public string AddedDateText => Added.HasValue ? Added.Value.ToString("d MMM yyyy") : "Added date unknown";
    }
}
