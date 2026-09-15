# Plan — doc-audit-agents

## Goal
Programmatically prove `docs/API_REFERENCE.md` has 100% parity with `Social/Social.API.json` (77 ops / 70 paths / 11 tags, every parameter), re-verify SDK doc snippets against generated code, then write a merged root `AGENTS.md` AI compass — and prove repo health (build, 242 tests, generate:all).

## Acceptance criteria (testable)
1. Temp audit script (in `$env:TEMP`, never in `plans/`) loads the spec and asserts: 70 paths, 77 ops, 0 operationIds, 11 tags; every METHOD+path in `API_REFERENCE.md`; every query/path param per operation present. Output logged; mismatches patched.
2. SDK claims re-verified: web hook shapes/names vs `sdks/web/endpoints/posts/posts.ts` + `user/user.ts`; Dart signatures vs `posts_api.dart`; `useGetFeedPosts`-style names (mission's guess) corrected to real `useGetApiPostsFeed`.
3. Root `AGENTS.md` exists, preserves existing portable rules, adds: mental model + ASCII maps, NEVER list, domain invariants, recipes A/B/C (DTO paths verified against repo reality, not the mission's guesses), 11-tag quick index.
4. Health: `dotnet build -c Release` 0 errors; `dotnet test -c Release` 242/242; `pnpm run generate:all` exit 0 from `sdks/generator`.
5. Findings committed (`docs:` fix commit if patches needed; `chore:`/`feat:` for AGENTS.md).

## Approach
1. Read existing root `AGENTS.md` + verify mission's recipe paths (DTO locations, migration project names) before writing.
2. Run node audit script via `shell` (temp dir); patch docs only on real mismatches.
3. Write `AGENTS.md` by merge (keep portable rules verbatim, append compass sections).
4. Health gates sequentially (build → test → generate); commit; close with review.md + SESSION_LOG.

## Scope
IN: audit script output, doc patches, AGENTS.md, health verification, commits.
OUT: code changes, SDK regen diffs (read-only unless pipeline broken), migration application, CI workflow files.

## Dependencies
- `Social/Social.API.json` fresh from prior `dotnet build`; node + pnpm + flutter + .NET 9 SDK available.
- No secrets. No destructive ops.

## Complexity
M (mechanical audit + one big careful file; two long-running gates).
