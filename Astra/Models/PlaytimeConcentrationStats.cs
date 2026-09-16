namespace Astra.Models
{
    /// <summary>How concentrated playtime is across games in a range - deliberately never carries game
    /// names/covers/ranking (spec section 17: "How concentrated was my playtime?" not "which games?" -
    /// that's Most Played's job). Shares are 0-100, each computed against TotalPlaytimeSeconds for the
    /// same range.</summary>
    public class PlaytimeConcentrationStats
    {
        public double Top1SharePercent { get; set; }
        public double Top3SharePercent { get; set; }
        public double Top5SharePercent { get; set; }
        public double Top10SharePercent { get; set; }
        public int TotalGamesTouched { get; set; }
    }
}
