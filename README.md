# Astra

A [Playnite](https://playnite.link) sidebar plugin for tracking your game stats and getting a yearly recap of your play habits — local-only, no accounts, no telemetry.

## What it does (v1)

- Tracks play sessions quietly in the background (start/stop, duration) — Playnite itself only tracks cumulative playtime, not individual sessions, so Astra keeps its own local log.
- A sidebar view shows a "Year in Review" recap: total hours played, session count, active days, most-played games, and new games added that year, navigable year by year.
- Export the recap as JSON.
- Clear all of Astra's tracked data at any time (does not touch your Playnite library or its own playtime stats).
- One-time, best-effort import of existing history from Lacro59's [GameActivity](https://github.com/Lacro59/playnite-gameactivity-plugin) plugin, if installed, so switching to Astra doesn't lose prior tracking.

See [FEATURES.md](FEATURES.md) for the full catalog of features considered (including what's deliberately excluded — hardware monitoring, cloud/AI/social features — and what's a candidate for a future version).

## Requirements

- Playnite (Desktop mode)
- Windows 10/11 (x86/x64/ARM64 covered by the bundled SQLite native binaries)

## Installation

1. Download/build `Astra.dll`, `extension.yaml`, `icon.png`, and the `runtimes\` folder together (see [TESTING.md](TESTING.md) for build instructions).
2. Copy them into a new folder under Playnite's `Extensions\` directory, e.g. `Extensions\Astra\`.
3. Restart Playnite. An "Astra" entry appears in the sidebar.

## Building from source

Requires the .NET SDK and MSBuild from Visual Studio Build Tools (this project builds WPF views on a classic .NET Framework target, which the `dotnet` CLI alone can't compile):

```
"C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\MSBuild.exe" Astra\Astra.csproj /t:Restore,Build /p:Configuration=Debug
```

Run the test suite:

```
dotnet test Astra.Tests\Astra.Tests.csproj -c Debug
```

Full build/test/manual-verification instructions: [TESTING.md](TESTING.md).

## Known bugs & limitations

See [BUGS.md](BUGS.md) — kept up to date after every change. Highlights: sessions interrupted by a hard crash aren't recorded (Playnite only reports elapsed time on clean stop), and GameActivity's on-disk import format is undocumented/reverse-engineered so a future GameActivity update could change it.

## Project status

Early development. Built, unit-tested, and confirmed loading cleanly in a live Playnite install (verified via Playnite's own log, no errors). Visual/interactive verification (sidebar rendering, multi-resolution, theme compatibility) is still outstanding — see BUGS.md's "Open items" section.

## Credits

Built with reference to four existing Playnite community plugins: [gs-playnite](https://github.com/game-scrobbler/gs-playnite), [playnite-gameactivity-plugin](https://github.com/Lacro59/playnite-gameactivity-plugin), [PlaytimeInsights](https://github.com/SHINKU1506/PlaytimeInsights), and [Playnite.YearInReview](https://github.com/SparrowBrain/Playnite.YearInReview).
