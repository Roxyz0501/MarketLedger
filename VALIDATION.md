# Validation — 2026-10-18 JST

## Result

- Clean Release build: 0 warnings / 0 errors, .NET 10, installed Dalamud API 15 SDK.
- Core tests: **653 passed**, including successful/failed/duplicate/ambiguous purchase notifications, HQ IDs, stale listings, wrong retainer, character/world switching, restart persistence, filters, exact 64-bit amounts, CSV escaping/formula protection/cancellation, cache invalidation and concurrent reads.
- Native UI tests: **2,468 passed**, using the actual ImGui bindings and Noto Sans CJK font assets with mocked Dalamud services. Includes seven languages, required glyphs in all four font faces, filter validation, zero-write rendering, and event/command cleanup.
- Japanese history and graph screenshots and German headers were visually inspected. Headers wrap; tables scroll horizontally and can be resized. These screenshots contain only synthetic data.
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

The 999 subsequent durable purchases in the 1,000-record fixture took **36,566 ms** on this workspace disk (~36.6 ms/transaction; not a game-host benchmark). Inserts use a background consumer and synchronous FULL transactions so this disk latency does not run inside the market callback. CSV and reads use background tasks. CSV serializes against inserts for a consistent snapshot; a very large export can delay queued inserts. The writer queue is limited to 256, and persistent write failures pause recording to bound memory. No artificial background purchases or live market actions were performed.

Network calls/bytes for ledger storage, viewing and export are **0 by design**, verified by the absence of HTTP/network clients in the implementation; this is not a network capture measurement. Runtime item data is read from the installed game sheets only. Physical database read/write byte counts and GPU frame time have not been measured.

The purchase page cache holds at most eight snapshots (100 rows, ≤366 buckets and ≤1,000 character entries each). Lifetime ends on a committed purchase, explicit reload, eviction or plugin unload. It does not expire by timer because this plugin owns the local store; another game process's writes require explicit reload. Listings expire after 10 minutes and cap at 1,000; unresolved requests expire after 30 seconds and cap at 16; item names cap at 2,048. Fonts keep observed glyphs with an 8,192-glyph reset threshold. No automatic history deletion is performed.

Cold aggregate queries scan rows matching the selected filter to compute exact totals; all-time queries and explicit all-results CSV exports intentionally cover all matching records. Only bounded aggregate results and pages are materialized, with character/time/world indexes. There are no per-row external calls. Schema version newer than 1 fails closed instead of changing data. The Windows SQLite localtime modifier returned NULL in this environment; a registered .NET date conversion function was implemented and tested instead.

## Remaining game-host verification

Actual market purchase notifications, purchase-time taxes (including zero tax), listing-name association, failed purchases, login/logout/world travel timing, and font rebuild timing under the live Dalamud atlas need in-game confirmation. The tests exercise actual managed/native UI code with synthetic data and mocked host services; they do not establish live game acceptance. Existing installed plugins have not been replaced.

The user authorized publication on 2026-10-18 JST. Version 1.0.0.0 is a public preview with the above game-host limitations. Final public repository, icon, Release asset and shared-index verification is recorded in PUBLICATION_STATE.md.
