Legacy manual SQL patches (pre-EF-migration era) — ARCHIVED.

Contents:
- 49 *_Manual.sql scripts (schema/permission/demo patches)
- 11 RUN_*.ps1 runners

Greenfield installs: Database:UseEfMigrations=true only (EF InitialCreate migration).
Upgrade from EnsureCreated DBs: set Database:ApplyLegacyPatchesAfterMigrations=true once, then false.

These files are retained for reference and manual DBA recovery — not executed by the application.
