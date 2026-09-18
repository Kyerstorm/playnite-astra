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
| Trend line/area charts (day/week/month/year) | ✅ | GameActivity, PlaytimeInsights |
| Calendar heatmap (playtime intensity by day) | ✅ | PlaytimeInsights |
| Weekday × hour-of-day heatmap | ✅ | PlaytimeInsights |
| Leaderboards (top games by playtime/sessions/avg length) | ✅ (year-scoped only, no all-time view) | PlaytimeInsights |
| Genre/platform/source breakdown charts | 🕓 | GameScrobbler, GameActivity |
| Time range selector + filtering by source/genre/tags | ✅ (Day/Week/Month/Year tabs, each showing one selectable single period via a dropdown - not filtered by source/genre/tags) | PlaytimeInsights |

## Trends / Playtime Insights (2026 extension)

Session-rhythm and activity-pattern analytics, deliberately separate from Most Played's
game-ranking responsibility — see CLAUDE.md's "Trends extension" section for the full
product-boundary rationale and per-section definitions.

| Feature | Status | Source |
|---|---|---|
| Gaming Rhythm (busiest weekday/month, longest/current streak) | ✅ | user-requested spec |
| Session Activity (count, avg/longest/shortest, sessions/active day) | ✅ | user-requested spec |
| Calendar activity heatmap (one year, session/game count tooltip) | ✅ | user-requested spec |
| Weekday distribution (Monday→Sunday, never sorted by value) | ✅ | user-requested spec |
| Hour-of-day distribution + Weekday × 3-hour-bucket heatmap | ✅ | user-requested spec |
| Session length distribution (5 fixed buckets, <30m through 4h+) | ✅ | user-requested spec |
| Streaks (longest/current/most active week) | ✅ | user-requested spec |
| Year-over-Year comparison (playtime/sessions/active days/games touched) | ✅ | user-requested spec |
| Playtime Concentration (top 1/3/5/10 share, games-touched count only) | ✅ (no game names/covers/ranking — Most Played's job) | user-requested spec |
| Game Rotation (new/returning games, multi-game days, counts only) | ✅ (no game list — Most Played's job) | user-requested spec |
| "Your Gaming Year" deterministic text summary | ✅ (fixed templates, no AI, no subjective language) | user-requested spec |
| Custom date range picker | 🕓 (spec allows deferring; presets cover the common cases) | user-requested spec |

## Yearly recap

| Feature | Status | Source |
|---|---|---|
| Annual summary (total hours, most-played, new games that year) | ✅ | YearInReview |
| JSON export of the recap | ✅ | YearInReview |
| Shareable image/card export | ✅ | YearInReview |
| Import a friend's shared report for comparison | 🕓 | YearInReview |

## Hardware/performance monitoring

| Feature | Status | Source |
|---|---|---|
| Per-session FPS/CPU/GPU/RAM/temp tracking | 🚫 | GameActivity |
| Configurable performance-warning thresholds | 🚫 | GameActivity |

## UI navigation & home experience (2026 redesign)

| Feature | Status | Source |
|---|---|---|
| Five-page sidebar navigation (Home / Trends / Most Played / Recap / Backlog) | ✅ | user-requested, inspired by mockup |
| Home: greeting + this-year stat cards + top 10 covers + "New this year" carousel | ✅ | user-requested |
| Most Played: top-25 ranked list with cover thumbnails, year-scoped | ✅ | user-requested |
| Real cover art (Playnite `Game.CoverImage`) with colored-initials fallback | ✅ | user-requested, mockup |
| Click a cover in Home's "Top Played" to jump to that game's details in Playnite's library view | ✅ | user-requested |
| Compatibility with Cover-Swapper's active cover (read live, never cache) | ✅ (`PlayniteGameInfoProvider` resolves `CoverImagePath` fresh every call, no caching) | user-requested |
| "Continue where you left off" recently-played row | 🕓 | brainstormed |
| "On this day" historical callback | 🕓 | brainstormed |
| Backlog burn-down % (owned-but-unplayed games, year-over-year trend) | ✅ (aggregate percentage only, no per-game list) | brainstormed |
| Never-played backlog page (browsable A-Z cover grid, all-time, genre/platform chip filters) | ✅ | brainstormed |
| Session cadence badge ("short bursts" vs "marathon sessions", from this year's session-length distribution) | ✅ | brainstormed |
| "Almost a milestone" (closest game to its next lifetime-hours threshold) | ✅ (single forward-looking callout only) | brainstormed |
| Full milestones/badges system (multiple earned badges, history, notifications) | 🕓 | brainstormed |
| Compare years (side-by-side stat deltas on Recap) | 🕓 | brainstormed |
| Search/filter on Most Played (platform/source/genre) | 🕓 | brainstormed |
| Grid/list density toggle on Most Played | 🕓 | brainstormed |
| Per-stat-card sparkline on Home | 🕓 | brainstormed |

## Data management

| Feature | Status | Source |
|---|---|---|
| Clear all locally tracked data | ✅ | (new, user-requested) |
| One-time import of existing GameActivity session history | ✅ (best-effort) | modeled on YearInReview's GameActivity dependency |
| Manual yearly playtime override per game (right-click a game in "Most played" → Edit playtime / Reset to tracked value) | ✅ | user-requested (lighter-weight variant of PlaytimeInsights' per-session manual editing — Astra corrects the yearly total rather than individual session rows) |
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
