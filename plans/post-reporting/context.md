# Post Reporting — Context

## Files to create
- `Social.Core/Entities/PostReport.cs` (new; string Id=Guid, PostId[255], ReporterUserId[255], Reason[32], Details[1000?], Status[16]=Pending, CreatedAt UTC, ReviewedAt?, ReviewedByAdminId[255?], AdminNote[500?]; navs Post, Reporter)
- `Social.Core/Reporting/ReportReasons.cs`, `ReportStatuses.cs` (new statics; reasons: Spam, Harassment, HateSpeech, Nudity, Violence, Misinformation, Copyright, Other)
- `Social.Core/Interfaces/IPostReportRepository.cs` (new: AddAsync, GetByIdAsync, GetMyReportsAsync, GetQueueAsync, ExistsOpenAsync, UpdateStatusAsync, GetOpenCountForPostAsync; all CancellationToken=default)
- `Social.Infrastructure/Repositories/PostReportRepository.cs` (new)
- `Social.Infrastructure/Migrations/*_AddPostReports.cs` (generated via dotnet ef; NOT applied)
- `Social.Application/Features/Reports/DTOs/*.cs` (ReportPostRequest, PostReportDto, ReportsPageDto, ResolveReportRequest)
- `Social.Application/Features/Reports/Commands/*.cs` + Validators + `Queries/*.cs`
- `Social.Tests/Unit/Repositories/PostReportRepositoryTests.cs`, `Social.Tests/Integration/Reports/PostReportingTests.cs`

## Files to modify (exclusive ownership per worker)
- Worker-A (persistence): `ApplicationDbContext.cs` (PostReport config only), `DependencyInjection.cs` (infra registration only)
- Worker-B (api): `PostsController.cs` (report routes only), `AdminModerationController.cs` (report routes only), `AdminRepository.cs`? NO — resolve reuses existing hide path via MediatR, no edit
- Worker-C (dashboard/docs): `Social/Controllers/Admin/AdminDashboardController.cs` (reports route + panel only), `Social.Admin.Web/wwwroot/css/admin-dashboard.css` (reports styles only), `docs/API_REFERENCE.md` (Posts §2 + Moderation §11 rows)

## Env / commands
- `dotnet build Social.sln -c Release` must precede any SDK regen; SDK regen itself is out of scope for workers (done at close if spec changed).
- `dotnet ef migrations add AddPostReports --project Social.Infrastructure --startup-project Social`; NEVER `dotnet ef database update` (needs human approval).
- `dotnet test Social.sln -c Release` baseline 242 must stay green.

## Open questions
- None blocking. Resolved: duplicate = same reporter+post with Status=Pending → 400 InvalidOperation; self-report → 400; blocked-author posts: still reportable (report is safety signal, not engagement) — no block gate on write, queue shows block context.
- Resolve actions: `dismiss` | `hide_post` (delegates to existing HidePostCommand semantics + notifies author) | `no_violation_restore`? Keep to dismiss/hide_post + note to stay minimal.

## Worker-A handoff (2026-09-16 — persistence, T1+T2)

### Created
- `Social.Core/Entities/PostReport.cs` — string GUID Id, required PostId/ReporterUserId/Reason, Details? (1000), Status="Pending" default, CreatedAt=UtcNow, ReviewedAt?, ReviewedByAdminId? (255), AdminNote? (500); navs Post?, Reporter(User)? via [ForeignKey].
- `Social.Core/Reporting/ReportReasons.cs` — Spam, Harassment, HateSpeech, Nudity, Violence, Misinformation, Copyright, Other.
- `Social.Core/Reporting/ReportStatuses.cs` — Pending, Dismissed, Actioned.
- `Social.Core/Interfaces/IPostReportRepository.cs` — exact 7 signatures per brief (AddAsync, GetByIdAsync, GetMyReportsAsync, GetQueueAsync, ExistsOpenAsync, UpdateStatusAsync, GetOpenCountForPostAsync; all `CancellationToken ct = default`).
- `Social.Infrastructure/Repositories/PostReportRepository.cs` — ValidatePage (page<1 → ArgumentException→400), NormalizeLimit (<1 → throw, clamp 50); ordering CreatedAt DESC, Id DESC; GetById/GetQueue Include Post+Reporter (queue read needs excerpts); UpdateStatus allowlists {Pending,Dismissed,Actioned} else ArgumentException, sets ReviewedAt=UtcNow, ReviewedByAdminId, AdminNote, missing id → KeyNotFoundException→404; GetOpenCountForPost counts Status==Pending; GetMyReports AsNoTracking without includes (list view, no post body needed).
- `Social.Infrastructure/Migrations/20260916225834_AddPostReports.cs` (+ Designer + ModelSnapshot) — purely additive: CreateTable PostReports + 4 indexes (IX PostId, IX ReporterUserId, IX composite PostId/ReporterUserId/Status, IX Status/CreatedAt DESC); both FKs Restrict; Down drops table. NEVER ran `database update`.

### Modified (exclusive files only)
- `Social.Infrastructure/Data/ApplicationDbContext.cs` — DbSet<PostReport> PostReports + §6b config only (lengths 255/255/32/16/1000/500/255; 4 indexes as above; Restrict on both navs; no global query filter; UTC/precision via existing global loop, not duplicated).
- `Social.Infrastructure/DependencyInjection.cs` — one line: `AddScoped<IPostReportRepository, PostReportRepository>()`.

### Verify
- `dotnet build Social.sln -c Release` → 0 errors (3 pre-existing warnings only). SQLite-free compile check per brief; no tests run (Worker-B/C own tests).
- Deviation: none from brief except exception type choice — used `ArgumentException` (exact, per brief) rather than PostRepository's `ArgumentOutOfRangeException` subclass; both map →400 via GlobalExceptionMiddleware. Did NOT touch controllers, AdminDashboardController, CQRS, docs, tasks.md.

## Worker-C handoff (2026-09-16 — dashboard + docs, T6 slice)
- Done: `AdminDashboardController.cs` — `[HttpGet("reports")] Reports()` → `RenderDashboard("reports")`; `activeReports` var; title `"reports" => "Trust & Safety · Post Reports"`; CONTENT sidebar entry `#nav-reports` (🚩 Reported Posts) after Moderation; `#tab-reports` panel (status pills All/Pending/Dismissed/Actioned, search box post-id/reporter/reason client-side, 8-col table Report/Post/Reporter/Reason/Status/Reported At/Open×/Actions, pagination bar); `#modal-report-inspect` + `#modal-report-resolve` (action select dismiss|hide_post + note textarea); JS `loadReports(status,page)` / `inspectReport(id)` / `openResolveModal`+`confirmResolveReport` / `quickDismissReport` + `resolveReport(id)` alias, all `fetch(..., {credentials:'same-origin'})` (admin_token cookie auto-sent), `escapeHtml` on excerpts/ids, `formatStandardDate` reuse, `showToast` reuse, empty-state row; `showTab`/popstate/init/cmdk extended for `reports`.
- Done: `admin-dashboard.css` — appended reports-only block (`.report-status-dot` + pending/dismissed/actioned colors, `.report-excerpt` 2-line clamp, `#tab-reports .stat-pill.active` rose tint). No other CSS touched.
- Done: `docs/API_REFERENCE.md` — 3 rows appended to Posts §2 + 3 rows to Moderation §11, table format matched. Headers/op-counts left untouched per brief (rows only).
- Done: `Social.Tests/Integration/Admin/ReportsDashboardRouteTests.cs` — anonymous `/admin/reports` → redirect `/admin/login`; admin cookie → 200 + contains "Reported Posts" + "Trust & Safety · Post Reports".
- Verified: `dotnet build Social.sln -c Release` → 0 errors (3 pre-existing warnings: 2× NU1903 AutoMapper advisory, 1× Redis fallback notice); new tests 2/2 green; full `Integration.Admin` suite 71/71 green.
- Assumption: backend report endpoints (Worker-B) don't exist yet — dashboard JS codes against the agreed contract and degrades to an error row until they land. Report `status` wire values assumed `Pending|Dismissed|Actioned` (match `ReportStatuses` statics).
- Did NOT touch: tasks.md (per brief), ApplicationDbContext, PostsController, AdminModerationController, CQRS, repos.

## Worker-B handoff (2026-09-16 — application+API, T3+T4+T5)

### Created (Application)
- `Social.Application/Features/Reports/DTOs/`: `PostReportDto` (Id, PostId, PostExcerpt?, PostAuthorId?, ReporterUserId, Reason, Details?, Status, CreatedAt, ReviewedAt?, AdminNote?), `ReportPostRequest` (Reason, Details?), `ResolveReportRequest` (Action, Note?), `ReportsPageDto` (Items, TotalCount, Page, PageSize, computed TotalPages).
- `Social.Application/Features/Reports/ReportMapping.cs` (internal): excerpt helper (first 140 chars), case-insensitive CanonicalReason/CanonicalStatus against `ReportReasons`/`ReportStatuses` statics, PostReport→PostReportDto (excerpt/author from nav when loaded).
- Commands: `ReportPostCommand(postId,reporterId,reason,details)→PostReportDto` (unfiltered `IAdminRepository.GetPostByIdAsync` read — no block/audience gate per resolved open question; missing/deleted→KeyNotFound 404; self→InvalidOperation 400; ExistsOpen→InvalidOperation 400; stores canonical reason casing); `CancelReportCommand(reportId,reporter)→bool` (missing→404; non-owner→UnauthorizedAccess 401; non-pending→InvalidOperation 400; hard delete via new DeleteAsync); `ResolveReportCommand(reportId,adminId,adminEmail,action,note)→PostReportDto` (missing→404; non-pending→400; dismiss→Dismissed / hide_post→`IAdminRepository.HidePostAsync`+Actioned with KeyNotFound if post vanished; writes AuditLog `ReportDismissed`/`ReportActioned`; notifies reporter always + author on hide, type `ModerationNotice`, resilient try/catch mirroring HidePostCommand).
- Queries: `GetMyReportsQuery(reporter,page,limit)`, `GetReportsQueueQuery(status?,page,pageSize)` (unknown status→ArgumentException 400; canonicalized before repo call), `GetReportByIdQuery(reportId)` — all → DTOs; paging throws/clamps inside repository (400 on page|limit<1).
- Validators (auto-wired via ValidationBehavior→400): `ReportPostCommandValidator` (reason ∈ 8, case-insensitive; details ≤1000, required when reason==Other); `ResolveReportCommandValidator` (action ∈ {dismiss,hide_post} case-insensitive; note ≤500).

### Modified
- `PostsController.cs` (additive only): `POST /api/posts/{postId}/report` [Authorize], `GET /api/posts/reports/mine` [Authorize] (`Page`/`Limit` capitalized per posts convention; literal route wins over `{postId}` — no reorder needed), `DELETE /api/posts/reports/{reportId}` [Authorize]. Specific catches (Validation→400, KeyNotFound→404, InvalidOperation/Argument→400, UnauthorizedAccess→401) because this controller wraps sends in try/catch (else 500); generic fallback preserved.
- `AdminModerationController.cs` (additive only, no try/catch per existing style — GlobalExceptionMiddleware maps): `GET /api/admin/moderation/reports` (status?,page,pageSize), `GET .../reports/{reportId}`, `POST .../reports/{reportId}/resolve`. Class-level `[Authorize(Roles="Admin,Moderator")]` covers all three.
- `Social.Core/Interfaces/IPostReportRepository.cs` (+1 additive method): `DeleteAsync(reportId, ct)→bool` — contract had no delete path but CancelReport requires hard delete (allowed: reports are not posts). Worker-A: keep this method when merging.
- `Social.Infrastructure/Repositories/PostReportRepository.cs` (+1 impl): `DeleteAsync` (false when missing; handler pre-checks so always true in flow). Also repaired a brace-merge artifact on `GetOpenCountForPostAsync` left from edit — verified clean.
- `Social.Tests/Infrastructure/TestPostReportRepository.cs` (new in-memory double, incl. DeleteAsync) + `CustomWebApplicationFactory.cs` (+2 lines: `PostReportRepositoryInstance` prop + `ReplaceScoped<IPostReportRepository>`). Worker-A: if you also registered this interface in the factory, keep one registration (mine backs all 22 tests below).
- `Social.Tests/Integration/Reports/PostReportingTests.cs` (22 tests, unique ids per test — report double is shared per class): user 200/duplicate-400/self-400/missing-404/deleted-404/anon-401/bad-reason-400/Other-nodetails-400/Other-ok-200/mine-paging/cancel-ok/cancel-foreign-401/cancel-missing-404; admin queue-200+excerpt/queue-status-filter/user-403/dismiss+audit/single-get/hide-post+IsDeleted+audit/double-resolve-400/missing-404/bad-action-400/cancel-after-resolve-400.

### Verify
- `dotnet build Social.sln -c Release` → 0 errors (145 pre-existing warnings only).
- `dotnet test --filter PostReporting` → 22/22 green. Full `dotnet test Social.sln -c Release` → 266/266 green (baseline 242 intact + 22 new + 2 dashboard-route tests from Worker-C).
- Did NOT touch: tasks.md, ApplicationDbContext.cs, AdminDashboardController.cs, CSS, docs.
