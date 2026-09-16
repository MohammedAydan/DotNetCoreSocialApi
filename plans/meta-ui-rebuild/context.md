# Context: meta-ui-rebuild

## Files to touch (solo, one at a time)
- T2 only: `Social.Admin.Web/wwwroot/css/admin-dashboard.css` (~61KB). Keep every existing selector working (retune values; additive for new helpers). Light-first `:root`, `[data-theme="dark"]` overrides.
- T3 only: `Social/Controllers/Admin/AdminDashboardController.cs` (~3050 lines). Regions: `<head>` fonts link, inline `<style>` `:root` + `[data-theme]` blocks, sidebar `<aside>` markup, navbar `<header>` markup, `GenerateLoginHtml` style+markup, inline `toggleAdminTheme`/`initAdminTheme`. Tab bodies (`tab-*` inner content), all fetch/DOM JS, modals, toasts: read-only except class-name alignment already covered by CSS.
- T4 only: `Social.Admin.Web/wwwroot/js/admin-dashboard.js` — theme block only (lines ~68-111): flip to dark-attribute semantics; do not touch palette/routes/loaders.

## Files to READ (no edits)
- `Social.Tests/Integration/Admin/AdminDocsRouteTests.cs` (markers that must survive: `tab-docs`, `Documentation`, `/admin/docs`).
- `Social.Tests/Integration/Admin/AdminAuthenticationTests.cs` (route/auth conventions).
- `docs/API_REFERENCE.md` §counts (docs tab copy stays truthful: 83 ops / 76 paths / 11 tags).

## Meta token contract (use exactly; grep-gated in T5)
- Light `:root`: `--bg-base:#F0F2F5; --bg-surface:#FFFFFF; --bg-card:#FFFFFF; --bg-card-hover:#F5F6F7; --border(-normal):#E4E6EB; --border-subtle:#CED0D433-ish; --text-primary:#050505; --text-secondary:#65676B; --brand:#0866FF; --brand-hover:#0B5CE6; --accent-blue:#0866FF; active-nav tint:#E7F3FF; success:#31A24C; warning:#F7B928; danger:#F02849;` font stack `-apple-system,'Segoe UI',Roboto,Helvetica,Arial,sans-serif`; radius 8/6/999; shadow `0 1px 2px rgba(0,0,0,.1)`.
- Dark `[data-theme="dark"]`: `bg #18191A, surface #242526, hover #303031, borders #3E4042, text #E4E6EB, secondary #B0B3B8, primary #2D88FF`, active-nav tint `rgba(45,136,255,.15)`.
- Keep legacy var names (`--accent-emerald/amber/rose/indigo/cyan/purple`, `--growth`, `--alert`, `--shadow-xs/sm`) mapped onto Meta hues so tab-body CSS keeps working.

## New deps / env vars
- None. No packages, no fonts (remove Inter `<link>`s), no env vars, no migrations.

## Open questions (decided)
1. Default theme light or dark? → Light (Meta is light-first; toggle persists `dark`).
2. Remove Inter webfont? → Yes; system stack is the Meta look and removes a render-blocking CDN.
3. Active nav style? → FB left-nav: `#E7F3FF` bg + `#0866FF` 600 text (not filled blue).

## Invariants (violate nothing)
- Routes, `RenderDashboard` auth, dual-delivery JWT, envelope, all `nav-*`/`tab-*` IDs, all inline JS function names, cmdk routes, docs markers, login IDs, `$$"""` brace escaping (single braces literal, `{{...}}` interpolates).
- 270 tests green; `sdks/**` untouched; 3 pending migrations stay pending; no secrets.
