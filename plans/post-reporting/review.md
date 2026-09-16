# Post Reporting — Review

## What was built
User-to-safety reporting for posts + admin triage console, delivered by 3 parallel workers (persistence / API / dashboard) with exclusive file ownership:
- Core: `PostReport` entity (GUID string Id, 255-capped FKs, Reason 32, Status 16, Details 1000, AdminNote 500, UTC precision-6 via global loop), `ReportReasons` (8 values) / `ReportStatuses` (Pending/Dismissed/Actioned), 7-method `IPostReportRepository` (+1 additive `DeleteAsync` for cancel).
- Infrastructure: `PostReportRepository` (ValidatePage throw<1, NormalizeLimit clamp 50, `CreatedAt DESC, Id DESC`, Restrict FKs, no global filter), DbContext §6b config, additive migration `20260916225834_AddPostReports` (NOT applied to prod).
- Application: `ReportPostCommand`+validator (canonical reason, Details required iff Other), `GetMyReports`/`CancelReport` (owner+Pending only), `GetReportsQueue`/`GetReportById` (canonical status, 400 on bad status), `ResolveReportCommand` (`dismiss`→Dismissed, `hide_post`→real `HidePostAsync`+Actioned; AuditLog `ReportDismissed`/`ReportActioned`; resilient reporter+author `ModerationNotice`s).
- API: 3 user routes on `PostsController` (capitalized `Page`/`Limit`), 3 admin routes on `AdminModerationController` (Admin,Moderator). Envelope + error matrix per compass (400 validation/dup/self/transition, 401 owner/anon, 403 roles, 404 missing/deleted).
- Dashboard: `GET /admin/reports` ("Trust & Safety · Post Reports"), 🚩 sidebar entry, queue table (status pills server-side, client search, inspect + resolve modals, toasts, escaped HTML, `formatStandardDate`, tabular-nums), appended CSS only.
- Docs: `API_REFERENCE.md` 77→83 ops / 70→76 paths (§2: 8→11, §11: 7→10); `AGENTS.md` + `README.md` pointers updated.

## Main-branch integration fixes (post-worker)
- `PostReportDto.OpenCountForPost` added; queue pre-computes per-distinct-post counts (page-bounded), single/resolve return live counts — dashboard Open× column is real.
- Dashboard `data.total ?? items.length` → `?? data.totalCount ??` (actual DTO field).
- `GetMyReportsAsync` + `Include(Post)` so "my reports" excerpts resolve.

## Edge cases handled
Duplicate open report (400), self-report (400), Other-without-details (400), cancel resolved/foreign (400/401), double-resolve (400), bad status/action (400), hide_post on missing post (404), anonymous (401), non-moderator admin routes (403), empty queue/search states, XSS-escaped excerpts/details.

## Verification
- `dotnet build Social.sln -c Release`: 0 errors (111 pre-existing warnings only).
- `dotnet test Social.sln -c Release`: 266/266 (242 baseline + 22 PostReporting + 2 dashboard-route).
- Fresh `Social/Social.API.json`: 76 paths / 83 ops / 11 tags; 6 report paths confirmed present.

## Known limitations / follow-ups
- Migration `AddPostReports` (+ 2 earlier pending) NOT applied to prod — needs explicit human approval + binary deploy.
- SDK regen (`pnpm run generate:all` from `sdks/generator`) not re-run; next regen picks up 6 new ops automatically.
- No auto-hide thresholds, no comment reporting, no email/push on resolution (in-app notices only).
- Queue open-counts are per-distinct-post lookups (≤50/page, admin-only) — acceptable; revisit with GROUP BY if pages grow.
