# Plan: admin-dashboard-overhaul

## Goal
Transform the embedded admin console (`/admin/*`, rendered by `Social/Controllers/Admin/AdminDashboardController.cs`) into a world-class management interface with a distinctive, elegant design system, plus an in-dashboard documentation section that mirrors the dashboard UI.

## Acceptance criteria (testable)
1. `dotnet build Social.sln -c Release` → 0 errors.
2. `dotnet test Social.sln -c Release` → all tests pass (266 baseline + new docs-route tests; 0 failures).
3. Admin auth preserved: anonymous `GET /admin` → 302 to `/admin/login`; non-admin cookie → 403; admin cookie → 200 on `/admin`, `/admin/users`, `/admin/moderation`, `/admin/reports`, `/admin/audit-logs`, `/admin/diagnostics`, and NEW `/admin/docs`.
4. New docs section: `GET /admin/docs` returns 200 with dashboard shell (sidebar/navbar), sidebar `📚 Documentation` entry active, in-page nav mirroring dashboard sections (Overview, Users, Moderation, Reports, Audit, System, API, SDKs, Runbooks), search filter works without backend call.
5. Design system: single token source (CSS variables, light + dark), Inter typography, responsive (sidebar collapses ≤768px), no external CSS framework added, Chart.js CDN retained.
6. No schema/migration change; no auth-contract change; no generated-tree edits (`sdks/**` untouched).

## Approach
- Keep the proven string-HTML shell architecture (ADR-011: Blazor tree stays dead code). Overhaul is CSS-token-first + surgical shell edits in `AdminDashboardController.cs` + JS enhancement in `Social.Admin.Web/wwwroot/js/admin-dashboard.js`.
- Three parallel workers with exclusive file ownership (no two agents touch the same file):
  - Worker-A (design-system): owns `Social.Admin.Web/wwwroot/css/admin-dashboard.css` — elegant token system, components, docs styles, responsive.
  - Worker-B (shell+docs): owns `Social/Controllers/Admin/AdminDashboardController.cs` — navbar/sidebar/login polish, `GET /admin/docs` route + docs tab HTML, sidebar entry, docs search/nav JS inline.
  - Worker-C (behavior+tests): owns `Social.Admin.Web/wwwroot/js/admin-dashboard.js` + new test file `Social.Tests/Integration/Admin/AdminDocsRouteTests.cs` — theme, command palette, docs filter, heartbeat/range wiring; route tests.
- Main agent integrates sequentially (B after A tokens finalized; C last), runs build + tests, screenshots via browser if feasible.

## Scope IN
- Design tokens (light/dark), sidebar/navbar/login/dialog/table/moderation/docs components in CSS.
- Shell polish + `/admin/docs` route + docs content (mirrors live UI sections + API/SDK/runbook pointers to `docs/`).
- JS: theme toggle, Cmd+K, docs filter, heartbeat/range (existing endpoints only).
- Route tests for `/admin/docs` (anon redirect, non-admin 403, admin 200 + content markers).

## Scope OUT
- No EF migrations, no new entities, no auth/token changes, no Blazor revival, no SDK regen, no Chart.js replacement, no new npm packages.
- No prod `database update`, no binary deploy (human approval required; out of scope).

## Dependencies
- Existing admin API endpoints (analytics, users, moderation, reports, audit, diagnostics) — read-only reuse.
- Existing test doubles (`TestAdminDoubles`, `TestAuthHandler`) for new route tests.
- `docs/API_REFERENCE.md`, `docs/ARCHITECTURE.md`, `docs/SDK_WEB.md`, `docs/SDK_MOBILE.md`, `docs/TOOLING_AND_PIPELINE.md` as docs-section source material (summarized, not duplicated verbatim).

## Complexity
- L (one 182KB controller + 32KB CSS + docs section; test-gated; parallel workers).
