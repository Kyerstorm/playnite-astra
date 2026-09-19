namespace Astra.Models
{
    /// <summary>Home's "achievements unlocked this week" highlight - null (via
    /// AchievementHighlightsService.FindRecentUnlocks) when there were none, rendered with
    /// TargetNullValue in HomeView.xaml rather than hiding the card, matching the existing
    /// highlights strip convention (see SessionCadence).</summary>
    public class RecentUnlocksHighlight
    {
        public int Count { get; set; }
    }

    /// <summary>Home's "rarest unlock ever" highlight - lifetime, not year-scoped. Null (via
    /// AchievementHighlightsService.FindRarestEver) when no unlock in the data has a rated
    /// GlobalPercentUnlocked.</summary>
    public class RarestUnlockHighlight
    {
        public string GameName { get; set; }
        public string AchievementName { get; set; }
        public double GlobalPercentUnlocked { get; set; }
    }
}
