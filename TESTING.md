# Testing

## Automated tests (per feature, not a monolithic suite)

Run only the test class mapped to the feature you're changing, unless you touched `AstraDatabase` (shared by everything — re-run all of them in that case).

| Feature | Test class | What it covers |
|---|---|---|
| Session recording | `SessionTrackerTests` | Correct duration/start-time math, zero/negative elapsed time ignored, sessions spanning midnight attributed to their start year |
| Yearly recap math | `RecapAggregatorTests` | Totals only include the selected year, top-games ranking, new-games-this-year filter, unknown game ID falls back gracefully, empty-year case |
| GameActivity import | `GameActivityImporterTests` | Missing install reported cleanly, real on-disk schema imports correctly, re-running is idempotent (no duplicate sessions), malformed files are skipped and reported rather than crashing, non-GUID filenames ignored |
| JSON export | `RecapExporterTests` | Recap round-trips through JSON without data loss |
| Manual playtime edit | `PlaytimeOverrideTests` (storage) + `RecapAggregatorTests` (aggregation) | Override round-trips and upserts rather than duplicating, is scoped to its own year, clears individually or via `ClearAllData`; recap applies an override in place of the computed sum, total reflects it, an override can change ranking order, unrelated games are unaffected |
| Leaderboard sorting | `LeaderboardSorterTests` | Sorting by playtime/sessions/average session length each produce the correct order; a game with zero sessions doesn't throw and sorts last under average session length |
| Playtime trends (day/week/month/year) | `TrendAggregationServiceTests` | Bucketing for each granularity, calendar (not rolling) month/year boundaries, Monday-start weeks, multiple sessions summed per period, sessions crossing midnight attributed to their start day only, empty periods kept as zero points, empty ranges don't throw, range boundaries (start inclusive/end exclusive), no duplicate counting, local-date interpretation, long ranges, average-per-period uses total periods not just active ones, `BuildMonthlyTrendForYear` matches the equivalent explicit `BuildTrend` call (this is what keeps Home's mini-chart and Trends' Month view in agreement), per-bucket session/game counts, leap year (366 daily buckets) |
| Trends analytics extension (rhythm, sessions, weekday, hour, session length, streaks, year comparison, concentration, rotation) | `PlaytimeInsightsServiceTests` | Busiest weekday picked by total playtime not session count; busiest month aggregated across years; longest/current streak (gaps break it, current ends on most recent active day, single-day streak); session count/avg/longest/shortest/sessions-per-active-day; weekday buckets always Monday-first regardless of which calendar week; all 5 session-length bucket boundaries (inclusive-low/exclusive-high) plus percentage-sums-to-100 and empty-range-is-zero-not-NaN; most active week by total playtime; 24 hour buckets by session start hour; 56-cell weekday×hour heatmap with correct day/bucket attribution; year comparison for both years independently, missing/zero previous year returns a real all-zero period not null; concentration top-1/3/5/10 share and capping at 100% with fewer than 10 games touched; rotation new-vs-returning classification and multi-game-day counting |
| Earliest-session lookup (Game Rotation's new/returning classification) | `AstraDatabaseTests` | Returns the true minimum `StartedAt` per game even when a later session was inserted first, ignores games outside the requested set, empty/null input short-circuits without querying |
| "Your Gaming Year" text generation | `GamingYearSummaryBuilderTests` | Singular/plural/zero grammar for days and games; busiest-month, streak, and session sentences match the spec's exact wording; each sentence is independently omitted when its underlying stat has no data (no crash, no "busiest month: null"); never contains subjective/motivational language; null analytics returns an empty list rather than throwing |

### Running tests

Run everything:
```
dotnet test Astra.Tests/Astra.Tests.csproj -c Debug
```

Run one class only (faster, targeted — the normal case while iterating on one feature):
```
dotnet test Astra.Tests/Astra.Tests.csproj -c Debug --filter "FullyQualifiedName~RecapAggregatorTests"
```

### Building the plugin itself

`dotnet build` alone cannot compile this project — it contains WPF `UserControl`/XAML on a classic .NET Framework target, which needs the full MSBuild toolchain (Visual Studio Build Tools), not just the .NET SDK:
```
"C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\MSBuild.exe" Astra\Astra.csproj /t:Build /p:Configuration=Debug
```
(Adjust the MSBuild path if Build Tools is installed elsewhere on your machine.)

## Manual verification (do this yourself after each feature lands)

### 1. Install into Playnite

1. Build the plugin (see above). Confirm `Astra\bin\Debug\Astra.dll`, `extension.yaml`, `icon.png`, and the `runtimes\` folder (SQLite native binaries) all exist.
2. Copy the entire `Astra\bin\Debug\` folder contents into a new folder under Playnite's `Extensions\` directory (e.g. `C:\Games\Playnite\Extensions\Astra\`).
3. Restart Playnite (it only loads extensions at startup).
4. Confirm a new "Astra" entry appears in the left sidebar with its icon visible.

### 2. Session tracking + recap

1. Launch any game from Playnite, let it run at least 30 seconds, then close it.
2. Open the Astra sidebar item.
3. Confirm the current year's stat cards updated: hours played, session count, active days all reflect that one session.
4. Confirm the game appears under "Most played."
5. Use the sort dropdown next to "Most played" to switch between Playtime / Sessions / AverageSessionLength. Confirm the list reorders correctly each time, and that each row shows hours, session count, and average session length together.

### 3. Year navigation

1. Click the "◀"/"▶" year controls.
2. Confirm the displayed year changes and the stats update (should show zeroed cards for years with no recorded sessions).
3. Confirm "▶" is disabled once you reach the current year (can't navigate into the future).

### 4. Export

1. Click "Export recap (JSON)", choose a save location.
2. Open the saved file and confirm it's valid JSON containing `Year`, `TotalPlaytimeSeconds`, `TopGames`, `NewGamesThisYear`.

### 5. Clear data

1. Click "Clear Astra data," confirm the warning dialog appears.
2. Confirm — stat cards should all reset to zero/empty for every year.
3. Confirm your Playnite library and each game's own Playtime/PlayCount are untouched (Astra only clears its own session table).

### 6. GameActivity import (only if GameActivity is installed)

1. Click "Import from GameActivity."
2. Confirm the status message reports a plausible imported-session count.
3. Click it again — confirm it now reports 0 imported / N skipped as duplicates (idempotent).
4. If GameActivity is *not* installed, confirm the status message says the source wasn't found, with no crash.

### 7. Multi-resolution

1. With Playnite windowed, resize it to roughly 1080p, then 1440p, then 4K-equivalent dimensions (or change Windows display scaling: 100%/125%/150%/200%).
2. At each size, reopen the Astra sidebar item and confirm: no clipped text, no overlapping stat cards, the top-games/new-games lists scroll rather than overflow.

### 8. Theming

1. In Playnite's Settings → Appearance, switch between at least: the default theme, a light-toned theme, and a true-black/OLED-style theme (if one is installed — the "Aniki ReMake" or similar community fullscreen themes ship dark-mode color variants; for Desktop mode any installed alternate theme works).
2. Reopen the Astra sidebar item after each switch and confirm text remains readable (no white-on-white or black-on-black), and card backgrounds/borders adapt rather than staying hardcoded to one theme's colors.

### 9. Edit / reset a game's yearly playtime

1. In "Most played," right-click any game's row (clicking anywhere on the row, including its title, should open the menu).
2. Choose **Edit playtime...**. Confirm the input dialog pre-fills with the game's current tracked hours.
3. Enter a new number (e.g. `8.8`) and confirm. The row's hours and the year's total stat card should update immediately.
4. Restart Playnite, reopen Astra, and confirm the edited value persisted (didn't revert to the original tracked sum).
5. Right-click the same game again, choose **Reset to tracked value**. Confirm it reverts to the original computed sum (the number it showed before step 3).
6. Try entering an invalid value (blank, negative, or non-numeric) — confirm an error dialog appears and the value is left unchanged.

### 10. Click a cover on Home's "Top Played" to open game details

1. On the Home page, hover a cover in the "Top Played" row — confirm the cursor changes to a hand.
2. Click it. Confirm Playnite switches to its own library view (not Astra's) with that exact game selected/highlighted.
3. Try this for a few different games in the row, including the 10th (last) one, to confirm it's not just the first cell that's wired up.

### 11. Trends navigation

1. Confirm the Astra nav rail now shows Home / Trends / Most Played / Recap, in that order, with Trends between Home and Most Played.
2. Click Trends. Confirm the page opens on Month granularity for the current year by default, with the "PLAYTIME" area chart, three summary cards (Total Playtime, Average / Period, Active Periods), and a `< year >` selector.

### 12. Day / Week / Month / Year granularity + ranges

1. Click DAY. Confirm the range dropdown offers "Last 7 days"/"Last 14 days"/"Last 30 days" (default 30) and the year selector disappears (Day is always relative to today).
2. Click WEEK. Confirm "Last 8/12/26 weeks" (default 12), year selector still hidden.
3. Click MONTH. Confirm "Current year"/"Previous year"/"Last 2 years" (default current year) and the `< year >` selector reappears; use it to change years and confirm the chart/cards update.
4. Click YEAR. Confirm "All years"/"Last 5 years"/"Last 10 years" (default All years), year selector hidden.
5. For each granularity, confirm switching range options recomputes the chart and all three cards without freezing Playnite, even with a large session history.

### 13. Tooltips and empty periods

1. Hover across the chart in Month mode for a year with at least one zero-playtime month. Confirm that month still appears on the chart as a visible zero point (not skipped), and hovering it shows "0m played" alongside its label.
2. Hover a month with real playtime — confirm the tooltip shows the correct month name/year and an "Xh Ym" value matching the stat cards' math.
3. Switch to Week/Day/Year and spot-check a couple of tooltips the same way (title format differs per granularity: "Week of D MMM", "Month D", or just the year).

### 14. Empty state

1. Pick a range/granularity combination with zero tracked sessions (e.g. Month / Previous year, on a fresh install, or Year / a range before any tracked data).
2. Confirm the chart area shows "No playtime yet" / "Play some games and your trends will appear here." instead of a blank or misleading chart.

### 15. Home mini-chart + View Trends navigation

1. On Home, confirm a "PLAYTIME TREND" section with a small area chart appears below "New this year," showing monthly playtime for the currently selected Home year.
2. Change Home's year with the `< >` controls — confirm the mini-chart updates to that year's monthly data.
3. Click "View Trends →". Confirm it navigates to the Trends page already showing Month granularity for the exact year Home was on.
4. Compare the mini-chart's shape/totals against the same year on the full Trends page (Month mode) — they must match exactly (both come from `TrendAggregationService.BuildMonthlyTrendForYear`).

### 16. Multi-resolution / theme (Trends-specific)

Repeat the checks from §7/§8 with the Trends page open: confirm the chart stays wide-and-shallow (not a tall vertical chart) at 1080p/1440p/4K and under 100–200% Windows scaling, and that the chart line/fill/gridline/tooltip all stay readable under at least one light and one dark Playnite theme.

### 17. Playtime Insights extension (Gaming Rhythm through Your Gaming Year)

1. Open Trends. Confirm the page now scrolls vertically below the chart (it no longer fits — and isn't meant to fit — in one viewport) and shows, in order: Gaming Rhythm + Session Activity (two columns), the calendar heatmap ("ACTIVITY"), Weekday distribution + Session Length distribution (two columns), "WHEN DO YOU PLAY?" (hour strip + weekday×hour heatmap), Streaks, Playtime Concentration + Game Rotation (two columns), Year-over-Year (Month/Year granularity only), and "YOUR GAMING YEAR" at the bottom.
2. Confirm the 4th summary card, SESSIONS, now appears alongside Total Playtime/Average/Active Periods and updates with range changes.
3. Gaming Rhythm: confirm Busiest Day/Month show a real weekday/month name with hours, and Current/Longest Streak show whole-day counts; on a range with zero sessions, confirm they show "—" / "0 days" rather than crashing or showing blank.
4. Session Activity: confirm Total/Average/Longest/Shortest/Sessions-per-day all update when you switch range presets, and that Longest/Shortest only ever come from sessions inside the currently selected range.
5. Calendar heatmap: hover a handful of cells across different months. Confirm the tooltip shows the full date, playtime (or "No sessions" for a zero day), session count, and distinct game count — never a game name. Confirm it renders a full year (365 or 366 cells) regardless of which granularity is currently selected on the main chart.
6. Weekday distribution: confirm the seven bars are always ordered Monday→Sunday even after changing ranges (never re-sorted by value), and hovering a bar shows its exact hours/minutes.
7. "WHEN DO YOU PLAY?": confirm the 24-bar hour strip and the weekday×hour grid both update with range changes, and hovering a heatmap cell shows its day + 3-hour window.
8. Session Length: confirm the five bars are labeled `< 30m`, `30m-1h`, `1-2h`, `2-4h`, `4h+` in that order, and hovering shows exact session count + percentage.
9. Streaks: confirm Longest/Current Streak match Gaming Rhythm's numbers, and Most Active Week shows a Monday date with its total playtime.
10. Switch to Month or Year granularity and confirm a Year-over-Year card appears (hidden in Day/Week) showing both years' Playtime/Sessions/Active Days/Games Touched side by side, with no red/green coloring, plus a 12-month grouped-bar comparison.
11. Playtime Concentration / Game Rotation: confirm only percentages and counts are shown — no game names, no cover art, no ranked list anywhere in these two cards.
12. "YOUR GAMING YEAR": confirm each sentence reads naturally (correct singular/plural), and that on a fresh install / zero-data range, the section either shows a single "You played on 0 days across 0 games." line or omits gracefully — never a crash or a "NaN"/"null" fragment.
13. Confirm none of the above ever shows a "Top Games" list, a game cover grid, or a leaderboard — that remains Most Played's page exclusively.
