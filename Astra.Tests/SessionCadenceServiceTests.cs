using System.Collections.Generic;
using Astra.Models;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class SessionCadenceServiceTests
    {
        // Index order matches PlaytimeInsightsService.SessionLengthBucketBoundaries:
        // 0 = <30m, 1 = 30m-1h, 2 = 1-2h, 3 = 2-4h, 4 = 4h+
        private static List<SessionLengthBucket> MakeBuckets(int under30m = 0, int b30mTo1h = 0, int b1hTo2h = 0, int b2hTo4h = 0, int over4h = 0)
        {
            return new List<SessionLengthBucket>
            {
                new SessionLengthBucket { SessionCount = under30m },
                new SessionLengthBucket { SessionCount = b30mTo1h },
                new SessionLengthBucket { SessionCount = b1hTo2h },
                new SessionLengthBucket { SessionCount = b2hTo4h },
                new SessionLengthBucket { SessionCount = over4h }
            };
        }

        [Fact]
        public void Describe_ShortBucketsDominant_ReturnsShortBursts()
        {
            var result = SessionCadenceService.Describe(MakeBuckets(under30m: 8, b30mTo1h: 4, over4h: 1));

            Assert.Equal("Short bursts", result.Label);
        }

        [Fact]
        public void Describe_LongBucketsDominant_ReturnsMarathonSessions()
        {
            var result = SessionCadenceService.Describe(MakeBuckets(under30m: 1, b2hTo4h: 5, over4h: 5));

            Assert.Equal("Marathon sessions", result.Label);
        }

        [Fact]
        public void Describe_TiedShortAndLongCounts_ReturnsBalancedMix()
        {
            var result = SessionCadenceService.Describe(MakeBuckets(under30m: 3, over4h: 3));

            Assert.Equal("Balanced mix", result.Label);
        }

        [Fact]
        public void Describe_AllSessionsInMiddleBucketOnly_ReturnsBalancedMix()
        {
            var result = SessionCadenceService.Describe(MakeBuckets(b1hTo2h: 10));

            Assert.Equal("Balanced mix", result.Label);
        }

        [Fact]
        public void Describe_NullBuckets_ReturnsNull()
        {
            Assert.Null(SessionCadenceService.Describe(null));
        }

        [Fact]
        public void Describe_WrongBucketCount_ReturnsNull()
        {
            var buckets = new List<SessionLengthBucket> { new SessionLengthBucket { SessionCount = 5 } };

            Assert.Null(SessionCadenceService.Describe(buckets));
        }

        [Fact]
        public void Describe_AllBucketsZero_ReturnsNull()
        {
            Assert.Null(SessionCadenceService.Describe(MakeBuckets()));
        }
    }
}
