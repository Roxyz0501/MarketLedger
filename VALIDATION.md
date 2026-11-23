# 1.2.0.2 — 2026-11-23

Canonical StandardPluginInfo.cs and StandardPluginTabs.cs copied byte-for-byte. Dependencies are empty: Gil Ledger uses host APIs and Windows SQLite, not another plugin. Allagan Local consumes this plugin data and is not a dependency. The trailing helper receives null for drawInfo when definition count is zero; the Information tab is fully hidden. Existing tabs and saved settings are preserved. Release build: zero warnings/errors. Native UI harness checks zero dependency definitions and zero installed-plugin enumeration. Live game verification remains outstanding. The test project emits MSB3246 for a host native DLL wildcard reference.

# Gil Ledger 1.2.0.1 review — 2026-10-25 JST

Reviewed recording/reconciliation, SQLite transactions and migration, filtering/totals/graphs, CSV, configuration backups, daily archive, metadata UI, lifecycle, localization and packaging. Tests use isolated synthetic data and mocked game services; live-game NPC hooks, market notifications and physical SSD IO remain unverified.

## Reproduced bugs and fixes

- Shutdown skipped joining CSV work when the query task faulted. Each task is now joined independently.
- Logout flush captured a UI synchronization context and could deadlock when Dispose synchronously waited. Both awaits now use ConfigureAwait(false); a queued UI-context regression reproduces the old behavior.
- Automatic recording pause discarded already observed pending wallet decreases. It now drains spending before clearing reconciliation state.
- A CSV temporary-file collision deleted a pre-existing .partial file. Cleanup now applies only to files created by this export.
- A cancelled zero-row export still produced a final CSV. Cancellation is checked before creating and before publishing the file.
- Recovery inserts accepted empty record IDs, out-of-range timestamps and inconsistent known-tax totals, which could break archive/query/export or alter totals. These rows are rejected before any transaction write.
- Date preset text used the Windows calendar while parsing expected Gregorian dates. Initial and preset dates now use invariant Gregorian formatting; verified with th-TH culture.

## Validation

- Core regression: 872 passed. Persistence/failure/restart: 57 passed. Native ImGui/glyph/lifecycle: 3,312 passed. New core review: 6 passed; lifecycle/culture review: 5 passed. Total: **4,252 passed**.
- Before fixes, all six new core assertions failed; three initial lifecycle assertions and the additional UI-context assertion failed. All now pass.
- Core fixture: 999 additional durable inserts took 50,813 ms. Cold view: 4 SELECTs; unchanged: 0 reads/writes; page navigation: 1 SELECT / at most 100 rows; 20 concurrent identical queries: 4 SELECTs total. No network calls by design.
- Persistence measurements unchanged: eight idle cycles 0 writes / 0 payload bytes; 120 edits coalesced to one save; three edited settings commits 143 payload bytes. Physical SSD writes were not measured.
- Shared Language/Support and HistoryFiles helpers are byte-identical to the canonical templates. Seven-language font rendering and UI lifecycle checks passed.
- Clean Release rebuild: zero warnings/errors. The 11-file allowlisted ZIP and manifest validation passed; public download results are recorded in PUBLICATION_STATE.md.

Commands (each uses a fresh test-results subdirectory):

```text
dotnet run --project Tests/Tests.csproj -c Release --no-restore -- test-results/review-core-20261025
dotnet run --project Tests/Tests.csproj -c Release --no-restore -- --persistence test-results/review-persistence-20261025
dotnet run --project Tests/Tests.csproj -c Release --no-restore -- --review test-results/review-20261025-fixed
dotnet run --project Tests/UI/UiTests.csproj -c Release --no-restore -- test-results/review-full-ui-20261025
dotnet run --project Tests/UI/UiTests.csproj -c Release --no-restore -- test-results/review-ui-context-20261025-fixed --review
```

## Historical validation records

# Gil Ledger 1.2.0.0 validation — 2026-10-23 JST

Persistence: 57 checks; core regression: 872 checks; native ImGui/font/lifecycle harness: 3,312 checks. Total: 4,241 passed. See PERSISTENCE_VALIDATION.md for conditions, measurements and limits. Clean Release build: zero warnings/errors. Live-game acceptance remains unverified.

The following is the historical 1.1.0.0 baseline.

# Gil Ledger 1.1.0.0 validation — 2026-10-18 JST

## Result

- Clean Release build: 0 warnings / 0 errors, .NET 10, installed Dalamud API 15 SDK.
- Core tests: **774 passed**, including successful/failed/duplicate/ambiguous market notifications; NPC request + money + item reconciliation in either event order; failed requests, overlapping requests, delayed market acknowledgements and logout ambiguity; residual outflows, transfers and unknown taxes; v1 database migration with backup and retained records; per-source search/graphs/CSV; character/world switching; restart persistence; CSV escaping/cancellation; cache invalidation and concurrent reads.
- Native UI tests: **2,905 passed**, using the actual ImGui bindings and Noto Sans CJK font assets with mocked Dalamud services. Includes seven languages, required glyphs in all four font faces, NPC filtering, all-outflows unknown-tax totals, both command aliases, filter validation, zero-write rendering and event/command cleanup.
- Japanese NPC-only, all-outflows and graph screenshots and German headers were visually inspected. Headers wrap; tables scroll horizontally and can be resized. These screenshots contain only synthetic data. NPC observer is intentionally unavailable in this mocked host, and the corresponding translated warning is visible.
- Canonical StandardPluginTabs.cs is byte-identical to the workspace template (SHA256 `062372ECBF0030427BCDFE471592C6691CE3A3D3F8835A7438EA95C7763B25E9`).
- NuGet restore, including audit metadata, completed with network permission. No advisory warnings were reported.
- Release DLL scan found no workspace path, Windows user path or local username string. Distribution uses an explicit file allowlist and excludes databases, recovery data, CSV exports, test fixtures and host DLLs.

## Access and performance

Measured by instrumented store counters on 1,000 synthetic purchases spanning 400 days, two characters. The counters below describe application SELECT executions and logical purchase inserts; they do not count SQLite pages/WAL bytes, schema initialization or duplicate-check queries inside an actual purchase transaction.

| Operation | Target | Measured |
| --- | --- | --- |
| Cold history | ≤4 SELECTs; ≤100 purchase rows | 4 SELECTs; 100 purchase rows, 366 aggregate buckets, 2 characters, 1 total row |
| 100 unchanged redraw/query requests | 0 SELECTs, 0 writes | 0 SELECTs, 0 inserts, 0 returned rows |
| Next uncached page | 1 SELECT, ≤100 purchase rows | 1 SELECT, 100 rows; cached summary/graph reused |
| Cached page/filter return | 0 SELECTs | cached result object reused |
| 20 concurrent identical queries | one shared query result | 4 SELECTs total |
| New successful purchase | 1 purchase insert, one transaction | 1 insert; duplicate listing/ID = 0 inserts; revision invalidates cache |
| Next display after purchase | refresh once, no polling | updated revision and totals verified |
| Search / navigation / chart / UI language drawing | 0 business writes | 0 purchase inserts, 0 config saves during rendering/navigation/filter tests |
| Filtered CSV | stream all matching records, bounded memory | 501 matching records across all pages; UTF-8 BOM verified |

The 999 subsequent durable purchases in the 1,000-record fixture took **38,043 ms** on this workspace disk (~38.1 ms/transaction; not a game-host benchmark). Inserts use a background consumer and synchronous FULL transactions so this disk latency does not run inside the market/inventory callback. CSV and reads use background tasks. CSV serializes against inserts for a consistent snapshot; a very large export can delay queued inserts. The writer queue is limited to 256, and persistent write failures pause recording to bound memory. No artificial background purchases or live market actions were performed.

Network calls/bytes for ledger storage, viewing and export are **0 by design**, verified by the absence of HTTP/network clients in the implementation; this is not a network capture measurement. Runtime item data is read from the installed game sheets only. Physical database read/write byte counts and GPU frame time have not been measured.

The purchase page cache holds at most eight snapshots (100 rows, ≤366 buckets and ≤1,000 character entries each). Lifetime ends on a committed purchase, explicit reload, eviction or plugin unload. It does not expire by timer because this plugin owns the local store; another game process's writes require explicit reload. Listings expire after 10 minutes and cap at 1,000; unresolved requests expire after 30 seconds and cap at 16; item names cap at 2,048. Fonts keep observed glyphs with an 8,192-glyph reset threshold. No automatic history deletion is performed.

NPC/outflow reconciliation is event-driven and capped: 64 NPC requests, 64 market reservations, 256 unmatched debits, 256 market credits, 512 item gains and 512 ready records. Entries expire after 30 seconds; a 250 ms in-memory drain runs only while pending data exists and never reads balances or the database. Overflow is reported visibly. Inventory move deltas are netted by item/quality across the batch. Exact same-batch retainer/FC deposits are separate transfers; all other unsupported causes stay unclassified. New source filters use a source/time index. Schema migration performs a one-time SQLite backup and transactional ALTER; subsequent schema-2 opens do not back up or rewrite the version. No business writes occur during browsing.

Cold aggregate queries scan rows matching the selected filter to compute exact totals; all-time queries and explicit all-results CSV exports intentionally cover all matching records. Only bounded aggregate results and pages are materialized, with character/time/world/source indexes. There are no per-row external calls. Schema version newer than 2 fails closed instead of changing data. The Windows SQLite localtime modifier returned NULL in this environment; a registered .NET date conversion function was implemented and tested instead.

## Remaining game-host verification

Actual market purchase notifications, purchase-time taxes (including zero tax), listing-name association, ShopEventHandler.ExecuteBuy resolution and shop-state layout, inventory/gil event ordering, NPC failed/coalesced purchases, deposits, login/logout/world travel timing, and font rebuild timing under the live Dalamud atlas need in-game confirmation. The hook uses the installed host's typed FFXIVClientStructs delegate and pointer; it observes the normal shop request and calls the original exactly once with unchanged arguments. Special currencies, exchange shops and buyback are not detailed NPC receipts. Simultaneous income/expense that cancels between inventory snapshots cannot be recovered. The tests exercise actual managed/native UI code with synthetic data and mocked host services; they do not establish live game acceptance. Existing installed plugins have not been replaced.

The user authorized publication on 2026-10-18 JST and requested NPC spending, source filtering and a matching name. Version 1.1.0.0 is a public preview with the above game-host limitations. InternalName, DLL, repository and config/database path remain MarketLedger for update compatibility; the display name is Gil Ledger. Final public repository, icon, Release asset and shared-index verification is recorded in PUBLICATION_STATE.md.
