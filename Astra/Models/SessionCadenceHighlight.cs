namespace Astra.Models
{
    /// <summary>Output of SessionCadenceService.Describe - a one-sentence characterization of
    /// how a year's sessions are distributed by length. Null when there were no sessions to
    /// classify (not a placeholder object).</summary>
    public class SessionCadenceHighlight
    {
        /// <summary>Short badge text, e.g. "Short bursts", "Marathon sessions", "Balanced mix".</summary>
        public string Label { get; set; }

        /// <summary>Full sentence, e.g. "You play in short bursts."</summary>
        public string Sentence { get; set; }
    }
}
