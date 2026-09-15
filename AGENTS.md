# AGENTS.md — Cross-Tool Project Rules

This file is the portable source of truth for AI coding agents (Antigravity, Cursor, OpenCode, Codex, Claude Code via import, etc.). Keep it focused, under ~150–300 lines of high-value non-discoverable facts.

Antigravity-specific overrides live in `GEMINI.md` (takes precedence inside Antigravity).

---

## Project invariants

- Prefer working, verified code over additional planning documents.
- Never invent secrets, credentials, keystores, or `.env` values. Escalate to human.
- Never run destructive or irreversible operations (migrations, force-push, mass delete, prod config) without explicit human confirmation.
- One in-progress task (`[~]`) per feature at a time across all agents.
- Update living docs (`plans/TECH_STACK.md`, `plans/ARCH.md`, `plans/DECISIONS.md`) in the same turn a change occurs.

---

## Planning discipline (anti-loop)

- A feature may have **exactly three** plan files before code starts: `plans/<feature>/plan.md`, `tasks.md`, `context.md`.
- After those three files exist, further planning documents are forbidden unless the human explicitly requests a re-plan.
- Maximum 8 new files under `plans/` per session.
- If an agent produces only documentation for 4 consecutive turns, it must stop and either implement or report a clear blocker.
- `/teamwork-preview` (or equivalent multi-agent teams) must finish planning quickly and transition to implementation; do not generate dozens of markdown files without code.

---

## Code quality baseline

- Strict typing. No `any`, no unchecked casts.
- Validate every input at system boundaries (API, CLI, form, queue, webhook).
- Handle every error path explicitly. No silent `catch {}` or swallowed rejections.
- Small, single-purpose functions. Composition over inheritance.
- Comments explain *why*, never *what*.
- No hardcoded secrets, URLs, or magic numbers — config/env only.

---

## Testing & verification

- A task is not complete until its tests run and pass against real output (not mocked/skipped).
- Co-locate tests: `foo.ts` → `foo.test.ts`.
- For UI changes, capture a screenshot as verification evidence.
- Prefer unit → integration → e2e only for critical flows.

---

## Git & safety

- Conventional commits: `<type>(<scope>): <what>` (feat, fix, refactor, chore, docs, test, perf).
- One logical change per commit. Never commit secrets, build artifacts, or `node_modules`.
- Prefer the project’s existing package manager, linter, and test runner. Do not invent new ones without updating living docs.

---

## When skills or specialist agents exist

- If a task matches an available skill (see `.agents/skills/` or equivalent), load and follow it.
- Prefer the dedicated implementation agent for already-planned scoped work.
- Do not expand scope silently; add new tasks instead.

---

## What does **not** belong here

Do not put long architecture narratives, full dependency lists, or file-by-file descriptions — agents discover those from the codebase. Keep this file to non-obvious commands, hard invariants, and anti-patterns.
