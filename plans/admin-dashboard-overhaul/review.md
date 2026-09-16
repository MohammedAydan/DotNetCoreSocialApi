# Review: admin-dashboard-overhaul

## What was built
- **Design system (Worker-A):** `Social.Admin.Web/wwwroot/css/admin-dashboard.css` 32KB → ~61KB. Unified token layer (surfaces, borders, text, brand, accents, shadows, radius scale, sidebar widths, mono font) with dark-default + `[data-theme="light"]`; refined sidebar/nav, sticky blurred navbar, metric/KPI cards, tables, badges, buttons, modals (`.open` modifier), toasts, moderation stream/grid/list, pills, view-switcher, charts, Cmd+K, profile menu, pagination, login set, footer, spinners; NEW docs styles (`.docs-layout/.docs-nav/.docs-article/.docs-card-grid/.docs-kbd/.docs-toc/.docs-callout`); responsive ≤768px/≤480px; `:focus-visible` rings, `.skip-link/.sr-only`, `prefers-reduced-motion` support. All pre-existing selectors preserved; pure CSS, no new deps.
- **Shell + docs section (Worker-B):** `Social/Controllers/Admin/AdminDashboardController.cs` — new `GET /admin/docs` → `RenderDashboard("docs")` (inherits anon→302 `/admin/login`, non-admin→403); `activeDocs` + title `Documentation · Admin Guide`; sidebar `DOCUMENTATION` group with `📚 Documentation` (`nav-docs`); profile-menu + Cmd+K entries; `tab-docs` section with 10 `.docs-article` blocks (Overview, Users, Moderation, Reports, Audit, Observability, API 83 ops/76 paths/11 tags, Web SDK, Mobile SDK, Runbooks & Deploy with pending-migrations warning) + `#docs-search` inline `filterDocs()` (no backend call) + `#docs-empty` state; `showTab`/popstate/cmdk bootstrap extended for `docs`.
- **Behavior + tests (Worker-C):** `Social.Admin.Web/wwwroot/js/admin-dashboard.js` → guarded vanilla-JS module (theme persist, Cmd+K palette incl. Documentation, `filterDocs`, range, heartbeat, reduced-motion; null-safe, no deps; `node --check` clean). NEW `Social.Tests/Integration/Admin/AdminDocsRouteTests.cs` — 4 tests (anon 302, non-admin 403, admin 200 + `tab-docs`/`Documentation`/`/admin/docs` markers, `/admin` no-regression).

## Verification
- `dotnet build Social.sln -c Release` → 0 errors (3 pre-existing warnings: AutoMapper advisory ×2, Redis-absent notice).
- `dotnet test Social.sln -c Release` → **270/270 passed** (266 baseline + 4 new docs-route tests).
- Grep evidence: `tab-docs`, `nav-docs`, `docs-search`, `filterDocs`, `/admin/docs` in controller; `.docs-layout/.docs-nav/.docs-article/.docs-card-grid/.docs-kbd/.docs-toc` in CSS.

## Edge cases handled
- Auth parity: docs route reuses `RenderDashboard` guards (no new auth path).
- Non-JS / missing-DOM: inline `filterDocs` + Worker-C module both null-safe; docs tab static (no fetch).
- Inline `<style>` vs CSS file: duplicated selectors keep aligned values; CSS file is a complete standalone replacement if inline block is ever removed.
- `$$"""` escaping audited (only legitimate `{{activeDocs}}` holes).
- No migrations, no API contracts, no `sdks/**` edits, no new packages.

## Known limitations / follow-ups
1. External `admin-dashboard.js` is still not referenced by the string-HTML shell (pre-existing wiring gap; inline script is source of truth). Wiring `<script src="/_content/.../admin-dashboard.js">` is a follow-up — verify last-wins global collisions first (both define palette/theme/filter fns, behavior-compatible but unverified together in browser).
2. Docs section is curated summaries + repo-path pointers (full tables stay in `docs/*.md`); no route serves `docs/*.md` (by design).
3. No live browser screenshot taken (server needs MySQL + admin seed); DOM-marker assertions + build/tests are the gate. Recommend human visual pass at `/admin/docs` (dark/light, 390px width) after deploy.
4. Pre-existing uncommitted work in tree (post-reporting entities, migrations, other session edits) untouched; commit scope should be reviewed before push.
5. Prod: 3 pending EF migrations still need explicit human approval + binary deploy (out of scope).
