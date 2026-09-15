using System;
using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class SessionTrackerTests
    {
        [Fact]
        public void RecordCompletedSession_WritesOneSessionWithCorrectDuration()
        {
            var db = TestDatabaseFactory.CreateTemp();
            var fixedNow = new DateTime(2026, 6, 1, 20, 0, 0);
            var tracker = new SessionTracker(db, () => fixedNow);
            var gameId = Guid.NewGuid();

            tracker.RecordCompletedSession(gameId, elapsedSeconds: 1800);

            var sessions = db.GetSessionsForYear(2026);
            var session = Assert.Single(sessions);
            Assert.Equal(gameId, session.GameId);
            Assert.Equal(1800, session.DurationSeconds);
            Assert.Equal(fixedNow.AddSeconds(-1800), session.StartedAt);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void RecordCompletedSession_IgnoresZeroOrNegativeElapsedTime(long elapsedSeconds)
        {
            var db = TestDatabaseFactory.CreateTemp();
            var tracker = new SessionTracker(db, () => DateTime.Now);

            tracker.RecordCompletedSession(Guid.NewGuid(), elapsedSeconds);

            Assert.Equal(0, db.GetSessionCount());
        }

        [Fact]
        public void RecordCompletedSession_SessionCrossingMidnightIsAttributedToItsStartYear()
        {
            var db = TestDatabaseFactory.CreateTemp();
            // Session started 23:50 on New Year's Eve, ran 20 minutes into the new year.
            var stoppedAt = new DateTime(2027, 1, 1, 0, 10, 0);
            var tracker = new SessionTracker(db, () => stoppedAt);
            var gameId = Guid.NewGuid();

            tracker.RecordCompletedSession(gameId, elapsedSeconds: 20 * 60);

            Assert.Empty(db.GetSessionsForYear(2027));
            Assert.Single(db.GetSessionsForYear(2026));
        }
    }
}
