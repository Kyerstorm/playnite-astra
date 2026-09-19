using System.Collections.Generic;
using System.Linq;
using Astra.Models;

namespace Astra.Services
{
    /// <summary>Pure, deterministic classification of a year's session-length distribution into
    /// one of three cadence badges - "short bursts" vs "marathon sessions" vs "balanced mix" -
    /// same fixed-template philosophy as GamingYearSummaryBuilder (no subjective/AI language,
    /// every sentence traceable to a number already computed elsewhere).
    ///
    /// Reads PlaytimeInsightsService.BuildSessionLengthBuckets' output by FIXED INDEX, not by
    /// label string, relying on that method always returning exactly 5 buckets in its documented
    /// order (&lt;30m, 30m-1h, 1-2h, 2-4h, 4h+) - the same kind of implicit-ordering contract
    /// already relied on elsewhere in this codebase (see CLAUDE.md's Weekday x Hour heatmap
    /// note on UniformGrid fill order). If the input ever isn't exactly 5 buckets, this returns
    /// null rather than guessing.
    ///
    /// "Short" = the &lt;30m and 30m-1h buckets combined; "long/marathon" = the 2-4h and 4h+
    /// buckets combined; the middle 1-2h bucket is excluded from both sides as a genuinely
    /// in-between length. Comparison is by SESSION COUNT, not total seconds, so cadence answers
    /// "how do you usually play" rather than "where did most of your hours go."</summary>
    public static class SessionCadenceService
    {
        public static SessionCadenceHighlight Describe(List<SessionLengthBucket> buckets)
        {
            if (buckets == null || buckets.Count != 5)
            {
                return null;
            }

            var totalSessions = buckets.Sum(b => b.SessionCount);
            if (totalSessions == 0)
            {
                return null;
            }

            var shortCount = buckets[0].SessionCount + buckets[1].SessionCount; // <30m, 30m-1h
            var longCount = buckets[3].SessionCount + buckets[4].SessionCount;  // 2-4h, 4h+

            if (shortCount == longCount)
            {
                return new SessionCadenceHighlight { Label = "Balanced mix", Sentence = "You play in a balanced mix of short and long sessions." };
            }

            return shortCount > longCount
                ? new SessionCadenceHighlight { Label = "Short bursts", Sentence = "You play in short bursts." }
                : new SessionCadenceHighlight { Label = "Marathon sessions", Sentence = "You play in long marathon sessions." };
        }
    }
}
