# Publication state

## 2026-10-22 — Local persistence implementation (not published)

- Implemented the audit follow-up in this original task folder: changed-only 60-second settings saves, three valid backups, corruption recovery, date-partitioned immutable transaction files and the canonical HistoryFiles tab.
- Existing WAL/FULL SQLite remains the durable all-date index and Allagan Local reader endpoint; incremental archive checkpointing avoids rebuilding the compatibility database.
- See PERSISTENCE_IMPLEMENTATION.md and PERSISTENCE_VALIDATION.md for reader contract, measurements and remaining game-host verification.
- No production data migration or installed DLL replacement was performed. Public release and shared index remain at 1.1.0.0; no public repository changes were made for this audit follow-up.

## 2026-10-18 — Gil Ledger 1.1.0.0 public preview

- Updated at the user's request to support NPC spending and source filtering; renamed display name from Market Ledger to Gil Ledger. Existing InternalName, DLL, repository, config path and database retained for update compatibility. New command /gilledger; /marketledger remains an alias.
- Repository: https://github.com/Roxyz0501/MarketLedger
- Release: https://github.com/Roxyz0501/MarketLedger/releases/tag/v1.1.0.0
- ZIP: https://github.com/Roxyz0501/MarketLedger/releases/download/v1.1.0.0/MarketLedger-1.1.0.0.zip
- Source/tag commit: `8bc13bfe9f471ddecf108cf67dd0899027698b9b`.
- ZIP: 112,141 bytes; SHA-256 `DF857EF33748BC6155512E2378E9032022260075AE367C31C5F06A0A36F4C39C`.
- Ordinary NPC gil-shop requests are reconciled with wallet decreases and matching item gains. Unclassified decreases and exact observed retainer/FC deposits are separate categories. Default totals include confirmed market/NPC purchases only. Source filters apply to history, totals, graphs and CSV; unknown tax is not represented as zero.
- Schema 2 migration creates a pre-migration backup and classifies existing records as market purchases. Seven languages and byte-identical canonical tabs preserved. Event-driven bounded reconciliation; no balance/database polling and no business writes on browsing.
- Clean Release: zero warnings/errors; 774 core and 2,905 native ImGui/glyph/lifecycle checks passed. NuGet restore/audit metadata succeeded with network permission. Public ZIP allowlist, manifest identity/URLs and binary local-path/credential-pattern scan passed.
- Actual in-game purchases, NPC native hook/shop layout, inventory event timing, deposits and live font behavior remain unverified. Special currencies, exchange shops and buyback do not have detailed NPC receipts. Public preview limitations are explicit in README, manifest and Release notes.
- Anonymous public repository and PNG icon HTTP 200 verified. Public ZIP hash matched the local package. Shared index commit `96e7aa75e4a4a2da09f0774c9c7b08daeec7c709`: pinned and normal main URLs both HTTP 200 with Gil Ledger 1.1.0.0 and correct download links; all ten other entries preserved.
- Source/index/publication-record writes used the GitHub connector; CLI used only for unsupported Release/asset operations, repository description and local Git synchronization. No installed plugin DLL was replaced.

## 2026-10-18 — Market Ledger 1.0.0.0 public preview

- Published at the user's explicit request; new standalone plugin, author Roxyz0501, MIT, Dalamud API 15.
- Repository: https://github.com/Roxyz0501/MarketLedger
- Release: https://github.com/Roxyz0501/MarketLedger/releases/tag/v1.0.0.0
- ZIP: https://github.com/Roxyz0501/MarketLedger/releases/download/v1.0.0.0/MarketLedger-1.0.0.0.zip
- Source/tag commit: `c95ee85a2c2b3b3d43916f6e172e002e4d388045`.
- ZIP SHA-256: `D386B147058E281D1E1F0CBC8E2E012AD98979F99BA72B974A20A710D6B25C88`.
- Per-character confirmed market purchases, retainer names when observed, tax-exclusive/tax/inclusive amounts, search/filter, daily/monthly/cumulative spending graphs and filtered CSV export. Local-only SQLite storage; no companion plugin dependencies. Seven UI languages and exact canonical trailing tabs.
- Clean Release zero warnings/errors; 653 core and 2,468 native ImGui/glyph/lifecycle checks passed. Unchanged views perform zero DB reads/writes; uncached page navigation reads at most 100 rows. In-game purchase/tax/world-travel/live-font acceptance remains unverified and is disclosed in the public prerelease.
- Anonymous repository, PNG icon and ZIP HTTP 200 verified. Downloaded ZIP hash, eight allowlisted entries and manifest author/version/RepoUrl/IconUrl verified.
- Shared index commit: `9a6fad59087a74b6e1f98b36f135d8c832c89c3f`. Connector and anonymous commit-pinned URL verified nine entries and correct download links, preserving all eight existing entries. Ordinary main URL initially served a cached eight-entry index.
- Source/icon/index/record writes used the connected GitHub integration. CLI used only for unsupported repository creation and Release/asset publishing. No installed plugin DLL was replaced.
