# Context — doc-audit-agents

## Known mission-brief deviations (verify, don't trust)
- "77 operationIds" — spec has **0** operationIds (verified last session); audit asserts 77 **operations**.
- `sdks/web/endpoints/posts.ts` — real path is `sdks/web/endpoints/posts/posts.ts` (tags-split).
- `useGetFeedPosts` — does NOT exist; real name is `useGetApiPostsFeed` (mutation-style, `{params}` variables).
- Recipe A "DTO in `Social.Core/DTOs/`" — suspect; real DTOs live under `Social.Application/Features/**/DTOs/` (verify via glob before writing AGENTS.md).
- Recipe B migration flags (`--project Social.Infrastructure --startup-project Social`) — plausible, verify project names exist.
- `docs/architecture.md` ≡ `docs/ARCHITECTURE.md` on this FS (case-insensitive overwrite found during commits; original in git history). New `AGENTS.md` has no such collision (no existing agents.md variants — verify).

## Sources
- Spec: `Social/Social.API.json` (build artifact; regen via `dotnet build` if stale)
- Docs: `docs/API_REFERENCE.md`, `docs/SDK_WEB.md`, `docs/SDK_MOBILE.md`
- Existing root `AGENTS.md` (portable rules — read fully before merging)
- `plans/DECISIONS.md` ADR-001–013 (AGENTS.md must reflect all)
- Audit script location: `$env:TEMP` only (never `plans/`, never repo)

## Learned (update during work)
- Commits `feat(sdk)` + `docs:` pushed for pipeline + portal; only foreign `production_schema_dump.txt` left uncommitted (untouched per standing rule).
