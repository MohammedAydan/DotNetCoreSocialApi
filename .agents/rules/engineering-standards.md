---
trigger: always_on
---
<!-- If your installed Antigravity/agy version does not read this frontmatter key,
     set this file's activation to "Always On" via the Rules panel (Customizations → Rules)
     or `.agents/rules` discovery instead — the body below is what matters. -->

# Engineering Standards

Full detail behind the non-negotiables in `GEMINI.md`. Applies to every file, every language, every agent (main or subagent).

## `plans/` — the persistent brain

```
plans/
├── context.md        # Project brain: purpose, status, constraints, active features, tech debt
├── SESSION_LOG.md     # Append-only session history — never overwrite old entries
├── ARCH.md            # System architecture (living)
├── TECH_STACK.md      # Full stack registry with versions + reasons
├── DECISIONS.md       # ADRs
├── PATTERNS.md        # Reusable patterns discovered while working
└── <feature-name>/
    ├── plan.md         # Goal, acceptance criteria, approach, in/out of scope, complexity [S/M/L/XL]
    ├── tasks.md        # [ ] pending  [~] in-progress (max ONE)  [x] done  [!] blocked: <why>  [-] cancelled: <why>
    ├── context.md      # Files touched, deps added, env vars, open questions
    └── review.md       # What was built, edge cases, limitations, follow-ups
```

**Templates** — `context.md`: Purpose / Current Status (active feature, health, date) / Critical Constraints / Active Features / Known Issues. **SESSION_LOG.md** entry: What was done / Decisions made (+ reason) / Files changed / State at end (feature, last task, next task, blockers) / Resume instructions. **DECISIONS.md** entry (`ADR-NNN`): Date / Status / Context / Decision / Alternatives considered / Consequences. **PATTERNS.md** entry: Problem / Solution / Example / Gotchas.

## Code quality

- Strict typing everywhere — no `any`, no implicit types, no unchecked casts.
- Validate every input at every system boundary: API, CLI, form, queue message, webhook.
- Handle every error path explicitly. No silent `catch {}`. No swallowed promise rejections.
- Small, single-purpose functions. Composition over inheritance.
- Comments explain *why*, not *what* — the code should already say what.
- No hardcoded secrets, URLs, or magic numbers — config/env only.

## Stack-specific gates (adjust as the real stack in `plans/TECH_STACK.md` evolves)

- **TypeScript**: `strict: true`; discriminated unions over boolean-flag soup; exhaustive `switch` via `never` check.
- **Next.js**: server components by default; audit every `'use client'` boundary; never let server-only secrets reach a client bundle.
- **NestJS / .NET Core**: layered (controller → service → repository); DI everywhere; DTO/pipe or model validation on every endpoint; guard/interceptor for auth on every protected route.
- **Firebase**: security rules reviewed for every collection touched in the diff; never trust a client-supplied ID for access control; batch multi-document writes for consistency.
- **Flutter**: use the project's *existing* state-management pattern — never introduce a second one; minimize widget rebuilds; handle platform-channel errors explicitly.
- **SQL**: parameterized queries only, never string concatenation; transactions for multi-row consistency; new query patterns get an index check.

## Debugging methodology (mandatory — no guess-and-patch)

1. **Reproduce** the failure reliably before touching anything.
2. **Isolate** — bisect to the smallest failing case.
3. **Hypothesize** a root cause; don't skip to a fix.
4. **Instrument** — logs/breakpoints/tests to confirm the hypothesis, not assumption.
5. **Fix the root cause**, not the symptom.
6. **Verify** against the original repro, then check the codebase for the same pattern elsewhere.
7. **Document** the root cause in `review.md` and, if it's a reusable lesson, in `PATTERNS.md`.

## Testing

- Unit: pure functions and business logic. Integration: API routes, DB queries. E2E: critical user flows only.
- Co-locate: `foo.ts` → `foo.test.ts`.
- Naming: `describe('ComponentName') > it('does X when Y')`.
- A task is not `[x]` until its tests actually run and pass — not mocked, not skipped.

## Git

- `<type>(<scope>): <what>` — types: `feat fix refactor chore docs test perf`.
- One logical change per commit. Never commit secrets, build artifacts, or `node_modules`.

## Security & permissions (Antigravity permission model)

Antigravity permissions are `action(target)` evaluated **Deny > Ask > Allow**. Configure explicitly rather than relying on defaults:

**Deny (hard-block, never override in-session):**
```
write_file(.env)                    write_file(**/*.keystore)
write_file(**/*.jks)                write_file(**/*.mobileprovision)
write_file(**/*.p12)                write_file(/home/*/.ssh)
write_file(.git/)                   command(sudo)
command(rm -rf)                     unsandboxed(regex:curl .*)
```
**Ask (always confirm — never cache "always allow"):**
```
command(*)                          execute_url(*)
mcp(sql/execute_mutation)           command(regex:.*migrate.*)
```
**Allow (safe, routine):**
```
command(git)                        command(regex:npm run (build|lint|test))
read_file(*)  (workspace-scoped)
```
- No secrets in code, comments, logs, or commit messages.
- No known-critical CVEs in new dependencies — check before adding.
- Verify session/auth on every protected route, every time — no "it was checked upstream" assumptions.

## Artifacts to produce

| Artifact | When |
|---|---|
| Plan | Before implementing any feature |
| Session Resume | Start of every session |
| Diff | After implementing |
| Verification (tests + screenshot for UI) | After testing |
| ADR | On any significant architectural decision |
| Blocker | Whenever something needs human input |
