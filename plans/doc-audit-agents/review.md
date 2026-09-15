# Review — doc-audit-agents

## What was done
- **Commits first:** `feat(sdk)` (pipeline isolation) + `docs:` (portal) pushed; only foreign `production_schema_dump.txt` left uncommitted (untouched per rule). Found + handled a case-collision: `docs/ARCHITECTURE.md` overwrote legacy `docs/architecture.md` (Windows FS case-insensitive; original safe in history); README now states the replacement explicitly.
- **Parity audit (programmatic, temp scripts in `$env:TEMP`):** spec = 70 paths / 77 ops / 0 operationIds / 11 tags / 200-only. Doc-level: 0 missing routes/params/bodies. Row-level: 1 miss (`unread` row said "same as above", literal `limit` absent) → patched to explicit params → **ROW-LEVEL PARITY 100%**.
- **SDK re-verification:** `useGetFeedPosts` confirmed non-existent (mission's guess wrong; docs correctly use `useGetApiPostsFeed`); `customInstance<void>` typing confirmed (envelope-cast pattern stands); Dart `apiPostsFeedGet({page, limit})` + `apiPostsPost({required createPostRequest})` + all-optional `CreatePostRequest()` ctor confirmed.
- **AGENTS.md merged:** existing 67-line portable rules kept verbatim; compass appended (mental model + 2 ASCII maps, 10-item NEVER list, invariants engine, recipes A/B/C with repo-true paths — DTOs in `Social.Application/Features/**/DTOs`, not `Social.Core/DTOs` — 11-tag index). No `agents.md` collision (verified no variants exist).
- **Health:** build 0 errors; test 242/242; `generate:all` exit 0 (idempotent: "wrote 0 outputs"); `flutter analyze` exit 0 (11 known warnings); `tsc` exit 0. Audit re-run on fresh spec: 100%.

## Edge cases
- Mission's "77 operationIds" corrected to 77 operations / 0 operationIds (asserted in script).
- Recipe B keeps the STOP rule: prod `database update` needs human approval (2 migrations still pending).

## Follow-ups
- Delete temp scripts? They're in OS temp, harmless. Re-run audits after any controller/DTO change.
- Still deferred: operationIds, Scalar UI, CI workflow wiring.
