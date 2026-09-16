# Plan: meta-ui-rebuild

## Goal
Rebuild the embedded admin console (`/admin/*`, rendered by `Social/Controllers/Admin/AdminDashboardController.cs` + `Social.Admin.Web/wwwroot/css/admin-dashboard.css`) on the Meta/Facebook system-design language: light-first FB palette, system typography, restructured sidebar/navigation, standardized buttons, and clean card/table display layout across every page including `/admin/docs` and `/admin/login`.

## Acceptance criteria (testable)
1. `dotnet build Social.sln -c Release` → 0 errors.
2. `dotnet test Social.sln -c Release` → 270/270 pass (zero regressions; no test edits).
3. Auth + routes untouched: anon `GET /admin*` → 302 `/admin/login`; non-admin → 403; admin → 200 on all 7 sections (covered by existing tests).
4. Contract markers preserved (grep-verified): `nav-overview/users/moderation/reports/diagnostics/audit/docs`, `tab-overview/users/moderation/reports/audit/diagnostics/docs`, `docs-search`, `filterDocs`, `Documentation`, `/admin/docs` sidebar link, login IDs (`login-form/email/password/btn-submit/login-alert/handleLogin`), JS globals (`navigateTab/showTab/setAdminRange/toggleAdminTheme/openCommandPalette/filterCommandPalette/pollAdminHeartbeat`).
5. Meta tokens present (grep-verified): `#F0F2F5` page bg, `#0866FF` primary, `#E7F3FF` active-nav tint, `#E4E6EB` borders, system font stack, light default with `[data-theme="dark"]` FB dark-mode (`#18191A/#242526/#E4E6EB/#B0B3B8/#2D88FF`) in all 3 theme spots (inline `<style>`, inline toggle/init, `admin-dashboard.js`).
6. No schema/migration change; no API-contract change; no `sdks/**` edits; no new packages/fonts (Inter link removed, system stack only; Chart.js CDN retained).

## Approach (solo, file-sequential)
- Keep the string-HTML shell architecture (Blazor tree stays dead code). Rebuild = Meta token layer + shell-markup restructure + component restyle; tab bodies keep their DOM contracts, only classes/tokens change around them.
- T2: rewrite `admin-dashboard.css` on Meta tokens (light `:root`, `[data-theme="dark"]` overrides), FB sidebar/nav, white navbar, FB button/form/badge/table/card/modal/toast/moderation/docs/login systems, responsive ≤768px.
- T3: controller shell — replace inline `:root`/`[data-theme]` token blocks (light-first), restructure sidebar (grouped caps sections, FB active-pill items, footer collapse) + navbar (FB search pill, icon buttons, profile chip) + login page (light card, blue CTA), flip inline `toggleAdminTheme`/`initAdminTheme` to `dark` semantics. All IDs/hrefs/onclick/JS names byte-identical.
- T4: flip `admin-dashboard.js` theme constant (`THEME_LIGHT='light'` → dark-attribute semantics) so external module matches the shell.
- T5: build + full tests + marker/token greps. T6: close.

## Scope IN
- Token system (both layers), sidebar, navbar, login, buttons, forms, badges, tables, metric/KPI cards, moderation cards, docs section, modals, toasts, pagination, Cmd+K, range switcher, theme toggle, responsive rules.

## Scope OUT
- No route/auth/API/schema/SDK changes; no tab-body JS logic changes; no new endpoints; no prod migration/deploy (human approval required, out of scope); no screenshots (no live server in this env — DOM-marker assertions are the gate).

## Dependencies
- Existing admin API endpoints (read-only reuse); existing test suite as regression gate (270).

## Complexity
- L (3000-line controller shell + 61KB CSS + theme-flip in 3 spots; test-gated; solo sequential).
