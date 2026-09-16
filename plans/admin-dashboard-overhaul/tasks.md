# Tasks: admin-dashboard-overhaul

- [x] T1 — Design-system CSS overhaul (Worker-A, owns `Social.Admin.Web/wwwroot/css/admin-dashboard.css` only)
- [x] T2 — Shell polish + `/admin/docs` route + docs tab (Worker-B, owns `Social/Controllers/Admin/AdminDashboardController.cs` only)
- [x] T3 — JS behavior upgrade (Worker-C, owns `Social.Admin.Web/wwwroot/js/admin-dashboard.js` only)
- [x] T4 — Docs-route tests (Worker-C, owns `Social.Tests/Integration/Admin/AdminDocsRouteTests.cs` only)
- [x] T5 — Main integration: sequential merge, `dotnet build -c Release` 0 errors, `dotnet test -c Release` all green
- [x] T6 — Visual verification (`/admin`, `/admin/docs`, light/dark, mobile width) + close (review.md, context.md, SESSION_LOG.md)
