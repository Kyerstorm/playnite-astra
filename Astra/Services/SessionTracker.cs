using System;
using Astra.Data;

namespace Astra.Services
{
    /// <summary>
    /// Records a session row whenever a game finishes a play (OnGameStopped
    /// gives us the elapsed time directly, so there's no need to track a
    /// "session in progress" state across app restarts).
    /// </summary>
    public class SessionTracker
    {
        private readonly AstraDatabase database;
        private readonly Func<DateTime> clock;

        public SessionTracker(AstraDatabase database, Func<DateTime> clock = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.clock = clock ?? (() => DateTime.Now);
        }

        /// <summary>
        /// Records a completed play session. <paramref name="elapsedSeconds"/> comes
        /// from Playnite's OnGameStopped event args. Sessions of zero or negative
        /// length (e.g. a game that failed to launch) are ignored.
        /// </summary>
        public long? RecordCompletedSession(Guid gameId, long elapsedSeconds)
        {
            if (elapsedSeconds <= 0)
            {
                return null;
            }

            var startedAt = clock().AddSeconds(-elapsedSeconds);
            return database.InsertSession(gameId, startedAt, elapsedSeconds);
        }
    }
}
