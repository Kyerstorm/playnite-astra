using System;

namespace Astra.Models
{
    /// <summary>Output of HomeHighlightsService.FindMostImproved - the returning game whose playtime
    /// grew the most versus the prior year, or null if no returning game improved.</summary>
    public class GameImprovementHighlight
    {
        public Guid GameId { get; set; }
        public string Name { get; set; }
        public string CoverImagePath { get; set; }
        public string PlaceholderColorHex { get; set; }
        public string PlaceholderInitials { get; set; }
        public long CurrentSeconds { get; set; }
        public long PreviousSeconds { get; set; }
        public long DeltaSeconds { get; set; }
    }
}
