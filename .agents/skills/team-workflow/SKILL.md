---
name: team-workflow
description: Defines the full session-boot ritual, the Project Bootstrap Protocol, the four-phase feature workflow (Plan, Implement, Verify, Close), the interruption protocol, and the decision rules for when to work solo versus delegate to a subagent, a custom .agents/agents/*.md agent, /boost, or /teamwork-preview. Use at the start of any session, before starting a new feature, or whenever deciding whether to delegate work.
---

# Team Workflow

This is how the project actually gets worked — solo by default, as a coordinated team only when it earns its cost. Every agent (main or subagent) that touches this repo follows this file.

## Session boot ritual (full version — see GEMINI.md §2 for the summary)

1. `view` `plans/context.md` — project purpose, status, active feature, constraints, known debt.
2. `view` the **last entry only** of `plans/SESSION_LOG.md` — don't read the whole history, you don't need it.
3. If resuming: `view` `plans/<active-feature>/plan.md`, `tasks.md`, `context.md`.
4. Produce a short Session Resume note: active feature, last completed task, next task, blockers.
5. Only then start work. Never write code before steps 1–4.

## Project Bootstrap Protocol (run once, when `plans/` doesn't exist)

1. Ask the human for: project name, purpose, tech stack, hard constraints.
2. Create `plans/context.md`, `plans/ARCH.md` and `plans/TECH_STACK.md` (fill what's known, mark the rest `TBD`).
3. Create empty `plans/DECISIONS.md` and `plans/PATTERNS.md`, ready for the first entry.
4. Create `plans/SESSION_LOG.md` with a first entry.
5. Report a Bootstrap Summary. Only then start feature work.

## Feature workflow

**Phase 1 — Plan (before any code).** Create `plans/<feature>/plan.md` (goal, testable acceptance criteria, approach, in/out of scope, dependencies, complexity S/M/L/XL), `tasks.md` (full breakdown), `context.md` (files to touch, new deps, env vars, open questions). Produce a Plan artifact. **Wait for human approval if the plan is destructive or touches a migration.**

**Phase 2 — Implement.** Mark a task `[~]` before starting it, `[x]` immediately after it's verified — never before. Update `context.md` as you learn things. Log new patterns to `PATTERNS.md`, architectural calls to `DECISIONS.md`, new deps to `TECH_STACK.md`, structural changes to `ARCH.md` — in the same turn they happen, not batched at the end.

**Phase 3 — Verify.** Run linter + formatter. Run tests (unit → integration → e2e as applicable) against real output — never accept a mocked or skipped pass as done. For UI work, capture a screenshot. Produce Diff + Verification artifacts.

**Phase 4 — Close.** All tasks `[x]` or `[-]` (with reason). Write `review.md` (what was built, edge cases handled, known limitations, follow-ups → spin into a new feature or tech-debt entry). Update `plans/context.md` (active feature, status, new debt). Append the `SESSION_LOG.md` entry.

## Interruption protocol

If a session ends unexpectedly: mark the in-progress task `[~]` with the exact sub-step reached; write precisely what was and wasn't done to `context.md`; write explicit resume instructions to `SESSION_LOG.md` ("Next session: do X, then Y, watch out for Z"); update `context.md` status to match reality — never leave it optimistic.

## Delegation decision tree — solo, subagent, or team?

**Default: solo.** Do the work yourself in the main thread. Most tasks — a single bug fix, a small feature, a focused refactor — don't benefit from delegation and only add coordination overhead.

**Dispatch a single subagent** (`invoke_subagent`, either a built-in `research`/`browser` agent or the custom `.agents/agents/software-engineer.md`) when *any* of these hold:
- The human explicitly asks for delegation or parallel work.
- The task is read-only research/exploration (codebase mapping, dependency tracing) that would otherwise bloat your own context — use the built-in `research` subagent for this, not `software-engineer`.
- There are two or more independent, **non-overlapping-file** units of work that can genuinely run concurrently.

Before dispatching: the subagent's prompt must point it at the exact `plans/<feature>/` files it needs — it starts with a clean context and does not inherit your conversation. After it returns (goes Idle), read its handoff, update `tasks.md`/`context.md` yourself if the subagent didn't, and don't leave it idle indefinitely — either close it out or re-awaken it with the next instruction.

**Escalate to `/teamwork-preview`** (Sentinel → Project Orchestrator → Explorers/Workers → Critic/Challenger/Auditor) only for what it's actually built for: large multi-file refactors or framework migrations, systems work needing continuous verification, or genuinely multi-milestone projects spanning days. It explicitly is *not* for a single self-contained fix — say "keep it small/focused" to force the lighter iterative path if it over-triggers. Pick an integrity mode (`development` for iteration, `demo` or `benchmark` for stricter, from-scratch verification) matching what the feature actually needs.

**Escalate to `/boost`** for one hard, self-contained problem in the current session — a gnarly concurrency bug, a tricky algorithm — that needs multi-angle deep reasoning but isn't a multi-day project.

**Coordination rules that apply to every dispatched agent, subagent or Teamwork worker alike:**
- Reads the same `plans/` brain before acting — no private undocumented state.
- Writes its outcome back into the same `plans/` files (`tasks.md`, `context.md`, `review.md`) — the human should be able to reconstruct the whole history from `plans/` alone, regardless of which agent did the work.
- **Exclusive file ownership**: no two agents edit the same file concurrently. Only one task may be `[~]` per feature at a time, across every agent working it.
- Nesting stays shallow — don't have a subagent spawn a subagent unless the task genuinely requires it (Antigravity caps nesting at 10 levels, but 1–2 is the practical ceiling for this project).
