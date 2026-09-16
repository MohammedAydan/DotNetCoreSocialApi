# Tasks: meta-ui-rebuild

- [x] T1 — Plan files (plan.md, tasks.md, context.md) — freeze after this turn
- [x] T2 — Meta token + component system in `Social.Admin.Web/wwwroot/css/admin-dashboard.css`
- [x] T3 — Controller shell rebuild in `Social/Controllers/Admin/AdminDashboardController.cs` (tokens, sidebar, navbar, login, theme flip)
- [x] T4 — Theme-constant flip in `Social.Admin.Web/wwwroot/js/admin-dashboard.js`
- [x] T5 — Verify: `dotnet build -c Release` 0 errors, `dotnet test -c Release` 270/270, marker/token greps
- [x] T6 — Close: review.md, context.md, SESSION_LOG.md
