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

---

# AI Agent Compass (repo-specific operating system)

> Read `docs/ARCHITECTURE.md` + `docs/API_REFERENCE.md` for depth. Below is the infallible minimum: violate nothing here.

## 1. Mental model & lifecycle maps

.NET 9 Web API + EF Core 9 + MySQL 8 + pnpm SDK monorepo (`sdks/generator` engine → `sdks/web` TS + `sdks/mobile` Dart). Tests: 266 (`Social.Tests`). Spec: `Social/Social.API.json` — 76 paths / 83 ops / 11 tags / 0 operationIds / bearer-only / 200-only responses.

```
Request lifecycle:
Client Request → RequestTelemetryMiddleware → TokenBlacklistMiddleware
  → GlobalExceptionMiddleware → Controller → MediatR ValidationBehavior
  → Handler → Repository (transaction + ExecuteUpdateAsync + split-query reads)
  → MySQL / Redis-or-memory cache          [envelope: {success,message,data,errors}]

Contract lifecycle:
C# DTOs (Social.Application/Features/**/DTOs) → dotnet build
  → Social/Social.API.json → cd sdks/generator && pnpm run generate:all
  → sdks/web + sdks/mobile
```

## 2. NEVER list (fatal failure modes)

- NEVER run `npm`/`yarn`, or create `package.json`/`node_modules` outside `sdks/generator/`. Root is .NET-only (ADR-013).
- NEVER run `pnpm run generate:all` before `dotnet build Social.sln -c Release` — the build emits the generator input.
- NEVER physical-`DELETE` a `Posts` row. Soft delete only: `IsDeleted = true` + `PostsCount − 1` (floored at 0).
- NEVER cascade-delete/orphan child shares. Keep `ParentPostId`; masking is in-memory via `MaskDeletedParentPosts` (`PostRepository.cs:441-463`).
- NEVER add a global `HasQueryFilter(p => !p.IsDeleted)`. Admin/moderation need unfiltered access; filter explicitly per query.
- NEVER remove `.AsSplitQuery()` on post reads. 3 ancestor levels × user × media = Cartesian explosion (comment at `PostRepository.cs:386-388`).
- NEVER invent `operationId`-based hook names. Spec has zero `operationId`s: web hooks are `use`+Method+Path (`useGetApiPostsFeed`, NOT `useGetFeedPosts`); body-writes are `useQuery`-style overloads, param-GETs are `useMutation` with `{params}` variables; all responses type as `void` — cast to the envelope.
- NEVER hand-edit generated trees (`sdks/web/**`, mobile `lib/src/**`, `.g.dart`). Web source: `sdks/generator/*`. Mobile factory source: `sdk-assets/`, never `lib/`.
- NEVER exceed `HasMaxLength(255)` on indexed string FKs (MySQL utf8mb4 3072-byte limit); `DateTime` always UTC + `SetPrecision(6)`.
- NEVER commit secrets, `node_modules`, build artifacts, or apply the 2 pending EF migrations without explicit human approval.

## 3. Domain invariants (rules engine)

- **Privacy:** `IsPrivate` author ⇒ only author + accepted followers (`Followers.Accepted`). `Visibility != Public` ⇒ author only (profile predicate `(isOwner || == Public)`, share/single use `OrdinalIgnoreCase`). Owner-of-deleted-parent still sees raw content; everyone else gets `"[This content has been deleted]"` + no media.
- **Block (both directions, everywhere):** `(me→them) || (them→me)` in `BlockUsers` ⇒ suppress feed/profile/single visibility, reject Follow/Like/Comment writes (`InvalidOperationException`→400), suppress notifications centrally in `NotificationRepository.AddAsync`. Profile reads: empty page; single reads: 401.
- **Share rejects when:** target missing/deleted (404) · `Visibility != Public` · author `IsPrivate` (null-safe: unknown ⇒ private) · any bidirectional block (all 400).
- **Counters:** `PostsCount`/`ShareingsCount` only inside `IDbContextTransaction` via `ExecuteUpdateAsync`; `rows == 0` ⇒ `throw KeyNotFoundException` (delete's post-row check returns `false` instead). Feed order `CreatedAt DESC, Id DESC`; `page|limit < 1` throws, `limit` clamps to 50; post params are `Page`/`Limit` (capitalized), elsewhere `page`/`limit`.
- **Errors:** Validation→400 (+`errors[]`), `Argument`/`InvalidOperation`→400, `UnauthorizedAccess`→401, `KeyNotFound`→404, `[Roles]`→403. 409 is not emitted anywhere. `IsVerified` is admin-only (profile edits ignore it).
- **Notifications:** write pipeline block → self-skip → preference toggle → quiet-defer → aggregate-or-insert; moderation notices bypass all of it. Inbox order `IsRead, Priority DESC, CreatedAt DESC`.

## 4. Runbooks

### Recipe A — add/modify an endpoint
1. DTO in `Social.Application/Features/<Area>/DTOs/` (+ FluentValidation validator; MediatR pipeline rejects invalid → 400).
2. Contract in `Social.Core/Interfaces/I<Area>Repository.cs`; implement in `Social.Infrastructure/Repositories/` (transaction for counter writes, two-way block predicate, `!IsDeleted`, `ValidatePage`/`NormalizeLimit`, `AsSplitQuery()` on post reads).
3. Action in `Social/Controllers/<Area>Controller.cs` with explicit auth + response types.
4. `dotnet build Social.sln -c Release` → `cd sdks/generator && pnpm run generate:all` → add tests in `Social.Tests/` → `dotnet test Social.sln -c Release` (242 baseline must stay green) → update `docs/API_REFERENCE.md` row.

### Recipe B — schema/index change
1. Entity in `Social.Core/Entities/`; configure in `ApplicationDbContext.OnModelCreating` (255-cap, precision-6, single-col FK indexes alongside composites — MySQL 1553).
2. `dotnet ef migrations add <Name> --project Social.Infrastructure --startup-project Social`; inspect diff; `dotnet ef migrations script --idempotent`.
3. STOP: `database update` on prod needs explicit human approval. Log ADR in `plans/DECISIONS.md`.

### Recipe C — SDK generation debugging
- `tsc` TS2307 on `zod`/`@tanstack/react-query` → `sdks/generator/tsconfig.json` `paths` map (typecheck-only; consumers use own deps).
- Orval zod `.default(null)` → null-default leaking from spec → fix at .NET transformer source, never sed generated output.
- Dart `build_runner` format fail → `generate-mobile.mjs` pubspec patch (`^3.8.0`) must run; factory missing → asset restore from `sdk-assets/` + `.openapi-generator-ignore` entry; `flutter analyze` must run **inside** `sdks/mobile/social_api_client`.

## 5. Tag index (11 tags → API_REFERENCE sections)

| Tag | Ops | Key params | Throws |
|-----|-----|-----------|--------|
| Posts §2 | 8 | `Page/Limit` caps; `{postId}`, `{userId}` | 400 share-policy/paging; 401 blocked/private; 404 deleted |
| Comments §3 | 7 | `{commentId}`, `{postId}` + `page/limit(d10)` | 400 block-gate; 404 missing parent |
| Like §4 | 2 | `LikeRequest{postId?}` | 400 duplicate/blocked; 404 |
| Follow §5 | 7 | `FollowRequest` | 400 blocked/self; 404 no relation |
| BlockUser §6 | 4 | `BlockUserRequest`, `blockedUserId?` | 400 self-block; 404 |
| Notifications §7 | 13 | `type?`, `unreadOnly?`, `page/limit` | 401 foreign; inbox `{items,total,unreadCount}` |
| User §8 | 14 | `SignIn{email,password}`, `q?`, `page/limit` | 401 credentials/lockout; +2 `/dashboard` aliases |
| AdminAnalytics §9 | 8 | `range?(30d)`, `days?(30)`, `take?(50)` | 401/403; snapshot-first reads |
| AdminAuditLogs §10 | 1 | `page/pageSize`, `actionType?`, `adminId?`, `from/toDate?` | 401/403 |
| AdminModeration §11 | 7 | `{postId}`, `{commentId}`, `reason?` | 401/403; hide/restore/visibility/perma-delete + notices |
| AdminUsers §12 | 6 | `{userId}` + ban/roles/verify/reset bodies | 400 reason/duration/Admin-target; instant JWT kill on ban |
