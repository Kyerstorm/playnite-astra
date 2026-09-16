namespace Astra.Models
{
    /// <summary>Factual counts only - never a game list (spec section 18). "New" means the game's
    /// earliest-ever recorded Astra session falls inside this range; "returning" means it was played
    /// in this range but also has a recorded session before the range started.</summary>
    public class GameRotationStats
    {
        public int GamesTouched { get; set; }
        public int NewGames { get; set; }
        public int ReturningGames { get; set; }
        public int DaysWithMultipleGames { get; set; }
    }
}
