---
trigger: always_on
---
<!-- 
  Antigravity Rules activation: Always On.
  If your version does not honour the frontmatter key, set activation to
  “Always On” via Customizations → Rules or `.agents/rules` discovery.
  Body below is authoritative.
-->

# Engineering Standards

Full non-negotiables referenced by `GEMINI.md` and `AGENTS.md`. Applies to every file, every language, every agent (main or subagent).

---

## `plans/` — the persistent brain

```
plans/
├── context.md          # Project brain
├── SESSION_LOG.md      # Append-only history (never overwrite)
├── ARCH.md             # Living system architecture
├── TECH_STACK.md       # Stack registry (version + reason)
├── DECISIONS.md        # ADRs
├── PATTERNS.md         # Reusable patterns
└── <feature-name>/
    ├── plan.md         # Goal, acceptance criteria, approach, scope, complexity
    ├── tasks.md        # [ ] [~] [x] [!] [-]
    ├── context.md      # Files, deps, env, open questions
    └── review.md       # Built, edge cases, limitations, follow-ups
```

**Strict limit**: each feature folder contains **exactly** the four files above (review appears after Close). No additional documents, no nested folders, no “options”, “exploration”, or “notes” files under `plans/`.

**File creation budget**: ≤ 8 new files under `plans/` per session. Exceeding is a hard stop.

**Templates** (concise):
- `context.md`: Purpose / Current Status / Critical Constraints / Active Features / Known Issues
- `SESSION_LOG.md` entry: What was done / Decisions (+ reason) / Files changed / State at end / Resume instructions
- `DECISIONS.md` (`ADR-NNN`): Date / Status / Context / Decision / Alternatives / Consequences
- `PATTERNS.md`: Problem / Solution / Example / Gotchas

---

## Anti-loop enforcement (mandatory)

- Plan phase produces exactly three files then ends.
- After the three plan files exist, further creation of planning documents is forbidden.
- Only `.md` files for 4 consecutive turns → stop and either implement or report blocker.
- `/teamwork-preview` must finish planning in ≤ 6 turns total and must not exceed 12 `.md` files under `plans/` without production code.
- Prefer updating existing plan files over creating new ones.
- Prefer writing production source over writing more documentation.

---

## Code quality

- Strict typing everywhere — no `any`, no implicit types, no unchecked casts.
- Validate every input at every system boundary.
- Handle every error path explicitly. No silent `catch {}`. No swallowed promise rejections.
- Small, single-purpose functions. Composition over inheritance.
- Comments explain *why*, not *what*.
- No hardcoded secrets, URLs, or magic numbers — config/env only.

---

## Stack-specific gates

Adjust as the real stack in `plans/TECH_STACK.md` evolves.

- **TypeScript**: `strict: true`; discriminated unions preferred; exhaustive `switch` via `never`.
- **Next.js**: server components by default; audit every `'use client'` boundary; never let server-only secrets reach a client bundle.
- **NestJS / .NET Core**: controller → service → repository; DI; DTO/pipe or model validation on every endpoint; guard/interceptor for auth on every protected route.
- **Firebase**: security rules reviewed for every collection touched; never trust client-supplied ID for access control; batch multi-document writes.
- **Flutter**: project’s existing state-management pattern only; minimize rebuilds; handle platform-channel errors explicitly.
- **SQL**: parameterized queries only; transactions for multi-row consistency; new query patterns get an index check.

---

## Debugging methodology (mandatory)

1. Reproduce the failure reliably.
2. Isolate to the smallest failing case.
3. Hypothesize a root cause; do not skip to a fix.
4. Instrument (logs / breakpoints / tests) to confirm the hypothesis.
5. Fix the root cause, not the symptom.
6. Verify against the original repro; search the codebase for the same pattern.
7. Document the root cause in `review.md` and, if reusable, in `PATTERNS.md`.

---

## Testing

- Unit: pure functions and business logic. Integration: API routes, DB queries. E2E: critical user flows only.
- Co-locate: `foo.ts` → `foo.test.ts`.
- Naming: `describe('ComponentName') > it('does X when Y')`.
- A task is not `[x]` until its tests actually run and pass — not mocked, not skipped.

---

## Git

- `<type>(<scope>): <what>` — types: `feat fix refactor chore docs test perf`.
- One logical change per commit. Never commit secrets, build artifacts, or `node_modules`.

---

## Security & permissions (Antigravity model)

Permissions evaluate **Deny > Ask > Allow**. Configure explicitly.

**Deny (hard-block)**:
```
write_file(.env)                    write_file(**/*.keystore)
write_file(**/*.jks)                write_file(**/*.mobileprovision)
write_file(**/*.p12)                write_file(/home/*/.ssh)
write_file(.git/)                   command(sudo)
command(rm -rf)                     unsandboxed(regex:curl .*)
```

**Ask (always confirm)**:
```
command(*)                          execute_url(*)
mcp(sql/execute_mutation)           command(regex:.*migrate.*)
```

**Allow (safe routine)**:
```
command(git)                        command(regex:npm run (build|lint|test))
read_file(*)  (workspace-scoped)
```

- No secrets in code, comments, logs, or commit messages.
- No known-critical CVEs in new dependencies — check before adding.
- Verify session/auth on every protected route every time.

---

## Artifacts to produce

| Artifact              | When                                      |
|-----------------------|-------------------------------------------|
| Plan (exactly 3 files)| Before implementing any feature           |
| Session Resume        | Start of every session                    |
| Diff                  | After implementing                        |
| Verification          | After testing (tests + screenshot for UI) |
| ADR                   | On any significant architectural decision |
| Blocker               | Whenever human input is required          |
| Review                | After Phase 4 Close                       |
