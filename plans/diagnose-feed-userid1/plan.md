# Feature Plan: Diagnose & Fix Feed Endpoint Unknown Column 'p.UserId1'

## Goal
Diagnose and eliminate the runtime error `Unknown column 'p.UserId1' in 'SELECT'` in the Feed endpoint by identifying whether the error originates from active source code, an undetected DbContext, compiled LINQ / AutoMapper projections, stale binaries, or an unrefreshed IIS / process deployment, and ensure the production endpoint queries `Posts.UserId` with HTTP 200.

## Acceptance Criteria
1. Full search identifies all occurrences and historical origins of `UserId1`.
2. Programmatic model inspection confirms 0 shadow properties and `UserId1` does NOT exist on `Post` in the active runtime EF model.
3. The exact Feed endpoint, handler, and query are traced, and the generated SQL is captured.
4. Clean rebuild and publish are verified to produce binaries with corrected mappings.
5. Verification confirms whether local execution produces clean SQL without `p.UserId1`.
6. Production IIS / deployment status is audited (timestamps, hashes, running process state).
7. Zero database schema modifications (no adding `UserId1` column to DB).
8. Feed endpoint verified returning HTTP 200 with valid data.

## Approach
- Step 1: Search repository (source, configs, binaries, obj, logs) for `UserId1`.
- Step 2: Inspect all DbContexts in the solution and verify which is injected into the Feed query.
- Step 3: Inspect the Feed query/handler/service/projections and capture generated SQL.
- Step 4: Programmatically inspect `context.Model` for `Post` properties and foreign keys.
- Step 5: Clean `bin`, `obj`, `publish`, rebuild and publish `Social.API` and `Social.Infrastructure`.
- Step 6: Execute local runtime test of the exact Feed endpoint and capture generated SQL.
- Step 7: Compare publish output hashes/timestamps against production deployment / IIS environment.
- Step 8: Check IIS process status and restart/recycle if running stale binaries.
- Step 9 & 10: Verify end-to-end and deliver final report.

## Scope
- IN: Solution source, models, query handlers, published binaries, diagnostic probes, local execution, deployment audit.
- OUT: Modifying production database schema (no adding dummy columns).

## Complexity
Medium-Large (Safety-critical production diagnostics and deployment synchronization).
