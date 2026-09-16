# Review: meta-ui-rebuild

## What was built (solo, Meta/Facebook system-design language)
- **Token system (both layers):** light-first `:root` (`#F0F2F5` bg, `#FFFFFF` surfaces, `#E4E6EB` borders, `#050505/#444950/#65676B` text, `#0866FF` brand, `#E7F3FF` active tint, `#31A24C/#F02849/#F7B928` semantics) + `[data-theme="dark"]` FB dark mode (`#18191A/#242526/#3A3B3C`, `#E4E6EB/#B0B3B8`, `#2D88FF`); system font stack everywhere; Inter webfont links removed (both pages); radius 8/6/999, soft FB shadows. Legacy var names kept and remapped so tab-body CSS keeps working.
- **Sidebar:** 280px white, blue-circle `S` brand mark, grouped caps sections, FB active-pill items (`#E7F3FF` + blue 600), `collapse-toggle` gray pill footer.
- **Navbar:** white sticky bar, new FB search pill (`navbar-search` → opens Cmd+K, zero new JS), segmented gray range control (white active), gray-pill Cmd+K trigger, circular theme toggle, solid-blue avatar, padded profile dropdown.
- **Buttons/forms:** 36px 600-weight system (filled blue primary, gray secondary, FB red/green/amber), gray-fill borderless inputs with blue ring focus, press-scale active state.
- **Display layout:** borderless white cards (metric/KPI/panel/diag/mod/docs) with soft shadows, FB feed rhythm (744px stream, 40px avatars, 15px body, semibold metrics), tinted pill badges, gray-hover tables, segmented pills switcher, FB dialogs/toasts/pagination, docs section restyled (active tint, brand links, input-gray code/kbd/callouts), light login card with blue `S` badge + blue CTA.
- **Charts:** line/donut/sparkline literals remapped to Meta hues; grid/tick colors readable on white (`rgba(0,0,0,0.06)`, `#65676B`).
- **Theme flip (3 spots):** inline `<style>` tokens, inline `toggleAdminTheme`/`initAdminTheme`, and `admin-dashboard.js` `THEME_DARK` — default is now light, `dark` persists. Stale `'light'` localStorage values safely fall back to light.

## Verification
- `dotnet build Social.sln -c Release` → 0 errors (only pre-existing AutoMapper-advisory + Redis-absent warnings).
- `dotnet test Social.sln -c Release` → **270/270 passed**, zero test edits.
- Greps: all `nav-*`/`tab-*` IDs, login IDs, JS globals, docs markers (`tab-docs`, `nav-docs`, `docs-search`, `Documentation`, `/admin/docs`) intact; Meta tokens present in controller + CSS; zero stale `Inter`/`data-theme="light"`/indigo hexes; `node --check` on JS clean.
- Scope: exactly 3 production files touched (CSS, controller, JS) + 4 plan files (≤8 budget).

## Edge cases handled
- Inline `<style>` loads after the CSS file, so both layers were flipped together — no dark/light mismatch.
- `$$"""` brace discipline kept (no new `{{` except existing interpolations).
- Collapsed-sidebar + mobile rules updated for 280px width and hidden search pill.
- Docs tab inline sticky-nav override (`background:var(--bg-surface)`) still correct on light.

## Known limitations / follow-ups
1. No live browser screenshot (no server in this env); DOM-marker assertions + full suite are the gate — recommend human visual pass at `/admin` + `/admin/docs` (light/dark, 390px width).
2. External `admin-dashboard.js` still unwired to the shell (pre-existing; inline script is source of truth).
3. Emoji nav icons retained (FB uses glyphs; a glyph pass is optional polish, out of scope).
4. Prod migrations (3) + binary deploy still need explicit human approval (out of scope).
