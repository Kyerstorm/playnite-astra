# Testing

## Automated tests (per feature, not a monolithic suite)

Run only the test class mapped to the feature you're changing, unless you touched `AstraDatabase` (shared by everything — re-run all of them in that case).

| Feature | Test class | What it covers |
|---|---|---|
| Session recording | `SessionTrackerTests` | Correct duration/start-time math, zero/negative elapsed time ignored, sessions spanning midnight attributed to their start year |
| Yearly recap math | `RecapAggregatorTests` | Totals only include the selected year, top-games ranking, new-games-this-year filter, unknown game ID falls back gracefully, empty-year case |
| GameActivity import | `GameActivityImporterTests` | Missing install reported cleanly, real on-disk schema imports correctly, re-running is idempotent (no duplicate sessions), malformed files are skipped and reported rather than crashing, non-GUID filenames ignored |
| JSON export | `RecapExporterTests` | Recap round-trips through JSON without data loss |

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
