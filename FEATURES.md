# Feature Catalog

Every feature identified across the four reference plugins researched for Astra, with its status. Update this whenever scope changes or a new feature idea comes up — it's the standing menu to revisit later, not a changelog.

Legend: ✅ In v1 · 🕓 Candidate for v2 · 🚫 Deliberately excluded

## Core tracking

| Feature | Status | Source |
|---|---|---|
| Session-level tracking (start/stop, duration) | ✅ | GameActivity, PlaytimeInsights |
| Manual session log editing (add/edit/delete/recover) | 🕓 | PlaytimeInsights |

## Dashboard & visualizations

| Feature | Status | Source |
|---|---|---|
| Overview stat cards (hours, sessions, active days, new games) | ✅ (subset) | PlaytimeInsights |
| Trend line/area charts (day/week/month/year) | 🕓 | GameActivity, PlaytimeInsights |
| Calendar heatmap (playtime intensity by day) | 🕓 | PlaytimeInsights |
| Weekday × hour-of-day heatmap | 🕓 | PlaytimeInsights |
| Leaderboards (top games by playtime/sessions/avg length) | 🕓 | PlaytimeInsights |
| Genre/platform/source breakdown charts | 🕓 | GameScrobbler, GameActivity |
| Time range selector + filtering by source/genre/tags | 🕓 | PlaytimeInsights |

## Yearly recap

| Feature | Status | Source |
|---|---|---|
| Annual summary (total hours, most-played, new games that year) | ✅ | YearInReview |
| JSON export of the recap | ✅ | YearInReview |
| Shareable image/card export | 🕓 | YearInReview |
| Import a friend's shared report for comparison | 🕓 | YearInReview |

## Hardware/performance monitoring

| Feature | Status | Source |
|---|---|---|
| Per-session FPS/CPU/GPU/RAM/temp tracking | 🚫 | GameActivity |
| Configurable performance-warning thresholds | 🚫 | GameActivity |

## Data management

| Feature | Status | Source |
|---|---|---|
| Clear all locally tracked data | ✅ | (new, user-requested) |
| One-time import of existing GameActivity session history | ✅ (best-effort) | modeled on YearInReview's GameActivity dependency |
| CSV export/import | 🕓 | GameActivity, PlaytimeInsights |
| Backup/restore, data-integrity checks | 🕓 | PlaytimeInsights, GameActivity |
| QuickSearch integration (filter library by playtime stats) | 🕓 | GameActivity |

## Social / AI / cloud (GameScrobbler)

| Feature | Status | Source |
|---|---|---|
| Cloud account + sync | 🚫 | GameScrobbler |
| AI-generated "gaming personality" / chat / roast | 🚫 | GameScrobbler |
| Recommendation engine | 🚫 | GameScrobbler |
| Achievement rarity tracking / multi-provider sync | 🚫 | GameScrobbler |
| Social integration (Steam friends, Discord, friend comparisons) | 🚫 | GameScrobbler |
| Network graph / "Universe" view | 🚫 | GameScrobbler |

**Why excluded:** user chose a fully local-only, privacy-first plugin — no accounts, no network calls, no telemetry. Revisit only if that constraint changes.
