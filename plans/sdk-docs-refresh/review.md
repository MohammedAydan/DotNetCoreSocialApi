# SDK + Docs Refresh — Review

## What was done
Regenerated both SDKs from the post-reporting spec and documented the 6 report ops with generator-verified symbols (solo, linear pipeline; zero production code touched).

## Verification
- `dotnet build Social.sln -c Release`: 0 errors (2 pre-existing NU1903 warnings); fresh `Social/Social.API.json` contains all 6 report paths.
- `pnpm run generate:all` (from `sdks/generator`): exit 0 — Orval web + dart-dio mobile incl. new `report_post_request` / `resolve_report_request` Dart models (+ tests/docs).
- `pnpm run typecheck`: 0 errors. `flutter analyze`: 0 errors, 11 upstream `unused_import` warnings (accepted generator-template noise per ADR-012/013; warnings exit non-zero, no errors).
- `dotnet test Social.sln -c Release`: 266/266 (unchanged, as expected — no prod code delta).

## Symbol evidence (all read from regenerated output, none invented)
- Web (`sdks/web/endpoints/posts/posts.ts`): `postApiPostsPostIdReport(postId, body)`, `usePostApiPostsPostIdReport` (useQuery-style), `useGetApiPostsReportsMine` (mutation `{params:{Page,Limit}}`), `useDeleteApiPostsReportsReportId`, key `['getApiPostsReportsMine']`.
- Web (`admin-moderation.ts`): `useGetApiAdminModerationReports` (mutation `{params:{status?,page?,pageSize?}}`), `useGetApiAdminModerationReportsReportId` (mutation `{reportId}`), `postApiAdminModerationReportsReportIdResolve(reportId, body)` + `usePostApiAdminModerationReportsReportIdResolve` (useQuery-style).
- Zod: `PostApiPostsPostIdReportBody` (`reason?`, `details nullish`), `GetApiPostsReportsMineQueryParams` (`Page d1`, `Limit d20` — capitalized), `PostApiAdminModerationReportsReportIdResolveBody` (`action?`, `note nullish`); all responses `zod.unknown()` → envelope-cast.
- Dart: `apiPostsPostIdReportPost({required postId, required reportPostRequest, …})`, `apiPostsReportsMineGet({page=1, limit=20 → query keys Page/Limit, …})`, `apiPostsReportsReportIdDelete`, `apiAdminModerationReportsGet({status, page=1, pageSize=20})`, `…ReportIdGet`, `…ReportIdResolvePost({required reportId, required resolveReportRequest})`; ctors `ReportPostRequest({this.reason, this.details})`, `ResolveReportRequest({this.action, this.note})`.

## Docs updated
- `docs/SDK_WEB.md`: tree counts (79 .ts files, 54 models), report hooks in architecture listing, new §3.5 reporting recipe.
- `docs/SDK_MOBILE.md`: 30 models + 30 `.g.dart`, posts row → 11 methods, admin-moderation report methods named, new §3.5 recipe (placed before §4), codegen counts.
- `docs/TOOLING_AND_PIPELINE.md`: gates line 242/242 → 266/266.
- `API_REFERENCE.md`: parity holds at 83 ops / 76 paths (no controller changes since post-reporting count).

## Follow-ups (out of scope)
Prod migration approval + binary deploy still pending (3 migrations); operationIds / Scalar UI / CI wiring unchanged.
