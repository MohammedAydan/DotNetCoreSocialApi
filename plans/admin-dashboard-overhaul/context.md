# Context: admin-dashboard-overhaul

## Files to touch (exclusive ownership)
- Worker-A: `Social.Admin.Web/wwwroot/css/admin-dashboard.css` (32KB; token system + components + docs + responsive). Must keep all existing IDs/classes referenced by controller JS (nav-*, tab-*, stat-*, kpi-*, exec-*, anomalies-body, cmdk-*, health-*, profile-menu, mod-*, admin-data-table, pagination-bar, modal-*, toast).
- Worker-B: `Social/Controllers/Admin/AdminDashboardController.cs` (182KB/2892 lines; owns shell + routes + docs). Add `[HttpGet("docs")]` → `RenderDashboard("docs")`; extend activePage switch (title "Documentation · Admin Guide", `activeDocs`); add sidebar `📚 Documentation` link; add docs tab `<div id="tab-docs">`; keep all existing routes/IDs/JS function names (`navigateTab`, `setAdminRange`, `openCommandPalette`, `toggleAdminTheme`, `loadExecutive`, …).
- Worker-C: `Social.Admin.Web/wwwroot/js/admin-dashboard.js` (2.7KB; theme/Cmd+K/docs-filter/heartbeat) + NEW `Social.Tests/Integration/Admin/AdminDocsRouteTests.cs` (anon→login redirect, non-admin 403, admin 200 + markers `tab-docs`, `Documentation`, sidebar link `/admin/docs`).
- Main only: `plans/admin-dashboard-overhaul/{tasks,review}.md`, `plans/context.md`, `plans/SESSION_LOG.md`, `plans/DECISIONS.md` (ADR only if architecture changes — not expected).

## Files to READ (no edits)
- `docs/API_REFERENCE.md`, `docs/ARCHITECTURE.md`, `docs/SDK_WEB.md`, `docs/SDK_MOBILE.md`, `docs/TOOLING_AND_PIPELINE.md` (docs-section source summaries).
- `Social.Tests/Integration/Admin/AdminAuthenticationTests.cs` (route-test patterns: AllowAutoRedirect=false, TestAuthHandler roles).
- `Social.Tests/Integration/Admin/ReportsDashboardRouteTests.cs` (latest route-test example for `/admin/reports`).

## New deps / env vars
- None. No npm packages, no NuGet packages, no env vars, no migrations.

## Open questions
1. Docs content depth: full API table dump vs curated section summaries with links to `docs/*.md`? → Decision: curated summaries + endpoint counts + links (avoid 20KB HTML bloat; full tables live in `docs/`).
2. Should inline `<style>` tokens in controller move to CSS file? → No (risky 2892-line diff); Worker-B only ADDS docs styles via existing classes + Worker-A tokens; inline `:root` stays as fallback.
3. Screenshots: browser tooling available? → Best effort; DOM-marker assertions in tests are the gate.

## Invariants (from AGENTS.md — violate nothing)
- Auth: `[Authorize]`-equivalent manual checks in `RenderDashboard` stay; dual-delivery JWT (`admin_token` cookie) untouched.
- Routes `/admin`, `/overview`, `/dashboard`, `/users`, `/moderation`, `/reports`, `/audit-logs`, `/audit`, `/diagnostics`, `/system`, `/login`, `/logout` unchanged.
- No physical DELETE of Posts; no global query filter; keep AsSplitQuery; 255-cap FKs; UTC dates.
- Envelope `{success,message,data,errors}` unchanged; no new API controllers.
- `sdks/**` generated trees untouched.
- 3 pending migrations stay pending (human approval required).
