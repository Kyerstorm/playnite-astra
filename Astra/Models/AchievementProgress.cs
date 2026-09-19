namespace Astra.Models
{
    /// <summary>Per-game achievement completion, resolved from PlayniteAchievements' own SQLite
    /// cache via IAchievementsProvider. Not null-wrapped here - absence of data for a game is
    /// represented by the game simply having no entry in the returned dictionary.</summary>
    public class AchievementProgress
    {
        public int Unlocked { get; set; }
        public int Total { get; set; }
    }
}
