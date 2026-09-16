# Astra — Claude Code Development Instructions

## Project context

Astra is a local-only Playnite Desktop plugin for tracking game sessions and presenting playtime statistics/yearly recaps. Keep the project privacy-first: no accounts, cloud sync, telemetry, or network calls unless explicitly approved.

The project targets .NET Framework 4.7.2 and WPF, and references PlayniteSDK. Build the plugin with the full Visual Studio MSBuild toolchain; `dotnet build` alone is not sufficient for the WPF plugin project.

## Feature implementation workflow — mandatory

Every feature must be implemented as a small, verifiable slice rather than as one large change.

For **every feature or sub-feature**:

1. Read `FEATURES.md`, `TESTING.md`, `BUGS.md`, and the relevant existing implementation before changing code.
2. Define the data model/API changes before UI work.
3. Implement the smallest complete vertical slice.
4. Add or update focused automated tests for the feature. If `AstraDatabase` changes, run the full test suite.
5. Build the plugin with MSBuild.
6. Run the relevant automated tests.
7. **Deploy/install the newly built plugin into the live Playnite instance before considering the feature implemented.**
8. Restart Playnite so the new extension assembly is loaded.
9. Perform the relevant manual verification in the actual Playnite instance, including UI/theme/resolution checks where applicable.
10. Check the Playnite log for extension errors/warnings related to Astra.
11. Fix any deployment/runtime/UI issues found during verification before moving on.
12. Update `TESTING.md` with repeatable manual verification steps when the feature introduces a new user-visible behaviour.
13. Update `BUGS.md` if a limitation or known issue is discovered.
14. Update `FEATURES.md` when the feature status changes.
15. Only then mark the feature complete in the implementation plan.

### Live Playnite deployment is not optional

Do not report a feature as "implemented" merely because the code compiles or unit tests pass. The required definition of done includes a successful deployment to the user's Playnite installation and manual verification in Playnite itself.

Use the existing deployment instructions in `TESTING.md` as the source of truth for the Playnite Extensions directory and required output files. Deploy the complete plugin output, not only the DLL: include `Astra.dll`, `extension.yaml`, `icon.png`, and the `runtimes\\` folder/native SQLite binaries as applicable.

If the Playnite installation path is unavailable or deployment cannot be performed, explicitly report that the feature is **not fully verified** rather than claiming completion.

## Trend charts feature — implementation plan

The planned Trend Charts feature adds a dedicated analytics section and reusable chart components, while also exposing a compact version on Astra Home.

### Scope

Supported aggregation levels:

- Day
- Week
- Month
- Year

Primary metric:

- Playtime

The architecture should allow additional metrics later (session count, active days, etc.) without coupling the chart UI directly to database queries.

### UX structure

Add a dedicated **Trends / Insights** section to Astra's internal navigation alongside Home, Most Played, and Recap.

The dedicated section should provide:

- Time-range selector appropriate to the selected aggregation level.
- Day / Week / Month / Year granularity selector.
- Total playtime for the visible range.
- Average playtime per period.
- Optional comparison to the immediately preceding equivalent period where meaningful.
- Line chart as the default visualization.
- Filled/area presentation as a visual mode where supported by the chart implementation.
- Hover/tooltips showing the exact period and playtime.
- Empty-state messaging when the selected range has no activity.
- Loading/non-blocking behaviour so large histories do not freeze the WPF UI.

Home integration should be intentionally compact:

- Add a **Playtime trend** card/section to Home.
- Default to the current year.
- Prefer a compact recent trend (for example the most recent 12 months or the current year's available months, depending on the selected Home context).
- Provide a clear action to open the full Trends / Insights section.
- Do not duplicate all filters and controls from the dedicated page on Home.

### Data architecture

Introduce a reusable trend query/aggregation service, separate from the WPF view models.

Suggested responsibilities:

- Fetch raw session rows for a requested date range.
- Bucket sessions into day/week/month/year periods using a single well-defined timezone/date policy.
- Sum elapsed seconds per bucket.
- Return ordered period/value DTOs suitable for both the dedicated chart and Home.
- Handle periods with zero playtime so the chart does not misleadingly skip dates/buckets inside a selected range.
- Keep database access off the UI thread.
- Avoid loading the entire history when only a bounded range is requested.

Do not calculate chart aggregates independently in Home and Trends. Both surfaces must consume the same aggregation service so their numbers cannot drift.

### Date/time rules

Use the same local-time interpretation already used by Astra's session/recap logic. A session belongs to the appropriate bucket based on its recorded local session start/date policy; do not introduce a second competing timezone rule.

For week aggregation, use one documented week-start convention consistently throughout Astra. Prefer the convention already established by existing recap/session logic if one exists.

For month and year aggregation, use calendar months/years rather than rolling 30/365-day windows.

### Performance requirements

- Database queries must be bounded by the selected range.
- Aggregation should occur off the WPF UI thread for non-trivial ranges.
- UI updates must marshal back to the dispatcher safely.
- Avoid recreating chart controls unnecessarily when only data changes.
- Avoid one database query per game or per bucket (no N+1 query pattern).
- If caching is introduced, invalidate it when a new session is recorded or data is cleared/imported.

### Chart implementation

Prefer a small reusable Astra chart control/view model rather than embedding chart-specific logic directly into each page.

The chart component should accept prepared trend data and presentation options. It should not know about `AstraDatabase` or Playnite APIs.

Keep the chart theme-aware and compatible with Playnite's light/dark/community themes. Do not hardcode colors that become unreadable on alternate themes.

Accessibility/UX requirements:

- Clear axis labels where useful.
- Tooltips for exact values.
- No dependence on colour alone to communicate values.
- Graceful rendering at 1080p, 1440p, 4K and Windows scaling of 100–200%.
- Avoid excessive point labels; use hover/tooltips for detailed values.

### Home integration rules

Home should use the same trend DTOs/service as the dedicated Trends page.

The Home chart must:

- Load quickly.
- Show a concise trend rather than a full analytics dashboard.
- Respect the current Home/year context.
- Never block the Home view while calculating history.
- Have a navigation affordance to open the full Trends / Insights page.

### Tests required

Add focused tests covering at least:

- Daily bucketing.
- Weekly bucketing.
- Monthly bucketing.
- Yearly bucketing.
- Sessions crossing midnight.
- Empty buckets within a non-empty range.
- Empty selected ranges.
- Multiple sessions in one bucket.
- Range boundaries/inclusive-exclusive date handling.
- Correct local-date interpretation.
- No duplicate counting.
- Home and dedicated view consuming the same aggregation results (where practical at the service/view-model boundary).
- Large-range aggregation without changing numerical results.

### Manual verification required

After each chart sub-feature lands, deploy it to the live Playnite instance and verify:

1. Trends / Insights opens from Astra's navigation.
2. Day, week, month and year aggregation each show correct values for known test data.
3. Changing the range updates the chart without freezing Playnite.
4. Hovering a point shows the expected period and playtime.
5. Zero-activity periods render correctly.
6. Empty ranges show a useful empty state.
7. Home shows the compact trend and links to the dedicated view.
8. Home and Trends show matching totals for the same range.
9. The chart remains readable at 1080p/1440p/4K and 100/125/150/200% scaling.
10. Verify at least one light and one dark/community Playnite theme.
11. Restart Playnite and confirm the feature still loads cleanly.
12. Check the Playnite log for Astra errors/warnings.

## Code-quality rules

- Preserve the existing local-only/privacy-first design.
- Do not add network dependencies for analytics or chart rendering.
- Keep database, aggregation, view-model, and view responsibilities separated.
- Prefer existing Astra patterns over introducing a second architecture.
- Keep changes backwards-compatible with existing Astra data where possible.
- Do not silently migrate/delete user data.
- Do not make unrelated refactors while implementing a feature.
- Keep error handling defensive: malformed historical data should not crash the Astra sidebar.

## Definition of done

A feature is complete only when:

- Code is implemented.
- Focused tests pass (and the full suite passes when shared database code changed).
- Plugin builds successfully with MSBuild.
- The built plugin has been deployed to the live Playnite installation.
- The feature has been manually verified in the live Playnite instance.
- Playnite logs have been checked.
- `TESTING.md`, `BUGS.md`, and/or `FEATURES.md` have been updated where appropriate.
- No known regression introduced by the feature remains unresolved.
