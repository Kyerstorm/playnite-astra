using System;

namespace Astra.Models
{
    public class Session
    {
        public long Id { get; set; }
        public Guid GameId { get; set; }
        public DateTime StartedAt { get; set; }
        public long DurationSeconds { get; set; }
    }
}
