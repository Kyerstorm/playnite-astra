# Known Bugs & Limitations

Updated after every work session. See [CLAUDE.md](CLAUDE.md) (not committed) for the full technical rationale behind these; this file is the committed, user-facing summary.

## Fixed during initial development (kept here for history/context)

- **Timestamp comparison bug (fixed)**: session timestamps were originally stored with `DateTime.ToString("O")`, which formats differently depending on `DateTimeKind`. This caused `GetSessionsForYear` to sometimes silently exclude sessions near a year boundary, and caused imported/recorded times to be off by exactly the local UTC offset on read-back. Fixed by switching to a fixed custom timestamp format and removing a spurious `ToLocalTime()` call. Caught by `SessionTrackerTests` and `RecapAggregatorTests` before release.
- **SQLite native library not found on .NET Framework (fixed)**: `Microsoft.Data.Sqlite` requires a native `e_sqlite3.dll`; on a classic .NET Framework target this isn't copied to the output folder by default. Fixed with an explicit `SQLitePCLRaw.bundle_e_sqlite3` package reference plus `CopyLocalLockFileAssemblies=true`. Verified the native DLL now deploys correctly next to `Astra.dll`.

## Known limitations (by design, not bugs)

- **Sessions interrupted by a crash are lost.** Astra records a session only when `OnGameStopped` fires (which is when Playnite reports `ElapsedSeconds`). If Playnite or the game crashes hard enough that `OnGameStopped` never fires, that play time is not recorded. This matches a fundamental limit of Playnite's event model, not something Astra can work around without its own polling/heartbeat (out of scope for v1).
- **Playnite doesn't track sessions natively** — only cumulative `Playtime`/`PlayCount`. Astra's own session log only covers time played *since Astra was installed*; historical playtime predating installation cannot be broken into individual sessions (only the GameActivity import path, where available, can backfill session-level history).
- **GameActivity import is best-effort.** The on-disk format (`ExtensionsData/<guid>/GameActivity/<gameId>.json`) is undocumented and was reverse-engineered from an installed v3.5 copy. A future GameActivity release could change this format silently; Astra reports files it couldn't parse (`FilesFailedToParse`) rather than crashing, but does not validate the *semantics* of a changed schema — malformed data resembling the old shape could import incorrectly. Import is always user-triggered, never automatic.
- **No net462 reference assemblies on this dev machine** — the project targets **net472** instead. Runtime-compatible with any Windows 10/11 install, but flagging as a deliberate deviation from the more common net462 target seen in other Playnite plugins.
- **Sidebar icon is a placeholder.** `icon.png` is a generated solid-color square, not a designed asset — cosmetic only.

## Open items pending manual verification

- Plugin has been built and unit-tested locally but **not yet installed into a real running Playnite instance**. Manual end-to-end verification (sidebar renders, session recording works live, settings persist, theme/resolution behavior) is the next step — see [TESTING.md](TESTING.md) for the exact steps.
- Multi-resolution (1080p/1440p/4K) and theme (light/dark/OLED) behavior has not yet been manually confirmed — layout uses proportional `Grid` sizing and `DynamicResource` brushes by design, but this needs eyes-on verification per TESTING.md.
