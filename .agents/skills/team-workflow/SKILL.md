---
name: team-workflow
description: >
  Defines session boot, bootstrap protocol, the four-phase feature workflow
  (Plan → Implement → Verify → Close), hard anti-loop rules, interruption
  protocol, and decision rules for solo vs subagent vs /teamwork-preview.
  Use at session start, before any new feature, or when deciding whether to
  delegate work. Prevents infinite planning loops and forces execution.
compatibility: Antigravity 2.0+, Antigravity CLI, OpenCode, agentskills.io compliant tools
metadata:
  version: "2.1"
  focus: anti-loop, execution-bias, plans-brain
---

# Team Workflow

This skill is how the project actually gets worked — solo by default, coordinated team only when the cost is justified. Every agent (main or subagent) follows it.

**Supreme rule**: Working, verified code is the goal. Planning is a means, not an end. Any agent that produces only planning documents without advancing to implementation is failing.

---

## When to use this skill

- Start of any session
- Before starting a new feature
- When deciding whether to work solo, dispatch a subagent, or escalate to `/teamwork-preview` / `/boost`
- When an agent appears stuck generating only `.md` files

## When NOT to use

- Pure one-line bug fixes that already have a clear plan
- Read-only questions that do not change the codebase

---

## Session boot ritual

1. View `plans/context.md`.
2. View the **last entry only** of `plans/SESSION_LOG.md`.
3. If resuming: view `plans/<active-feature>/{plan,tasks,context}.md`.
4. Emit a short Session Resume note: active feature · last completed task · next task · blockers.
5. Only then begin work. Never write code before steps 1–4.

---

## Project Bootstrap Protocol (once, when `plans/` is missing)

1. Ask the human for: project name, purpose, tech stack, hard constraints.
2. Create exactly: `plans/context.md`, `plans/ARCH.md`, `plans/TECH_STACK.md` (fill known values, mark rest TBD).
3. Create empty `plans/DECISIONS.md` and `plans/PATTERNS.md`.
4. Create `plans/SESSION_LOG.md` with a first entry.
5. Report a Bootstrap Summary. Then start feature work.

Budget: ≤ 6 files total for bootstrap.

---

## Feature workflow (four phases)

### Phase 1 — Plan

Create **exactly three files** under `plans/<feature>/`:

| File | Content |
|------|---------|
| `plan.md` | Goal, testable acceptance criteria, approach, in/out of scope, dependencies, complexity (S/M/L/XL) |
| `tasks.md` | Ordered, concrete tasks with status markers |
| `context.md` | Files to touch, new deps, env vars, open questions |

Produce a Plan artifact.

**Hard rules**:
- Exactly three files. No nested plan folders, no exploration documents, no option matrices as separate files.
- Maximum 4 turns refining the three files. Then freeze.
- Wait for human approval **only** if destructive or touches a migration. Otherwise proceed immediately to Phase 2.
- Never create extra `.md` files “to think more clearly”. Update the existing three.

### Phase 2 — Implement

- Mark a task `[~]` before starting, `[x]` only after verified.
- Update `context.md` as you learn.
- Log patterns → `PATTERNS.md`, architecture → `DECISIONS.md`, deps → `TECH_STACK.md`, structure → `ARCH.md` in the same turn.
- Prefer writing production source code over more documentation.

### Phase 3 — Verify

- Run linter + formatter.
- Run the tests implied by acceptance criteria against **real** output (never accept mocked/skipped as done).
- UI work → screenshot.
- Produce Diff + Verification artifacts.

### Phase 4 — Close

- All tasks `[x]` or `[-]` (with reason).
- Write `review.md` (what was built, edge cases, limitations, follow-ups).
- Update `plans/context.md`.
- Append `SESSION_LOG.md`.

---

## Hard anti-loop rules (non-negotiable)

1. **File creation budget**: ≤ 8 new files under `plans/` per session. Exceed → hard stop, report to human.
2. **Plan termination**: once the three files exist, Phase 1 is finished. Further planning without human request is forbidden.
3. **Documentation-turn limit**: only `.md` files for 4 consecutive turns → stop and either implement the first `[ ]` task or report a blocker.
4. **No re-planning** without explicit human “re-plan” or “change the plan”.
5. **`/teamwork-preview` kill switches**:
   - Allowed only when human explicitly requests it **and** scope is large (multi-file refactor, framework migration, multi-milestone).
   - Across all workers, planning must finish in ≤ 6 turns.
   - ≥ 12 `.md` files under `plans/` with zero production code → every worker stops, plan is frozen, orchestrator forces first implementation task or surfaces blocker.
   - Prefer the lighter solo / single-subagent path. Signal “keep it small/focused” if Teamwork over-triggers.
6. **Execution bias**: prefer code over documentation.

---

## Interruption protocol

Mark in-progress task `[~]` with exact sub-step; write what was/wasn’t done to `context.md`; write precise resume instructions to `SESSION_LOG.md`; never leave status optimistic.

---

## Delegation decision tree

**Default: solo.** Most tasks do not benefit from delegation.

**Dispatch a single subagent** when any of:
- Human explicitly asks for delegation or parallel work.
- Read-only research that would bloat main context → use built-in `research` subagent.
- ≥ 2 independent, non-overlapping-file units of work that can run concurrently.

Before dispatch: point the subagent at the exact `plans/<feature>/` files. After return: read handoff and update `tasks.md`/`context.md` if needed.

**Escalate to `/teamwork-preview`** only for large multi-file refactors, framework migrations, systems work needing continuous verification, or multi-milestone projects. Not for single self-contained fixes.

**Escalate to `/boost`** for one hard, self-contained problem needing multi-angle deep reasoning (not multi-day).

**Coordination rules for every dispatched agent**:
- Reads the same `plans/` brain before acting.
- Writes outcomes back into the same `plans/` files.
- Exclusive file ownership — no two agents edit the same file concurrently.
- Only one `[~]` task per feature at a time.
- Nesting stays shallow (practical ceiling 1–2).

---

## Status markers in `tasks.md`

```
[ ] pending
[~] in-progress (maximum ONE across all agents)
[x] done (only after verification)
[!] blocked: <reason>
[-] cancelled: <reason>
```

---

## Anti-patterns (never)

- Creating more than the three required plan files
- Spending > 4 turns refining a plan
- Creating exploration / options documents
- Recursive or nested planning
- Staying in Plan phase after the three files exist
- Dispatching Teamwork for small/focused work
- Exceeding the 8-file `plans/` budget
- Producing only documentation for 4+ consecutive turns
- Marking `[x]` before real verification
