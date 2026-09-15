# Review — enterprise-docs

## What was built
Five enterprise docs + README portal, all evidence-based (4 parallel subagent scans → main-agent synthesis):
- `docs/ARCHITECTURE.md` — invariants with `PostRepository.cs` / `ApplicationDbContext.cs` line refs (masking :441-463, share policy :46-120, block predicates, ExecuteUpdate counters, indexes, split-query depth-3).
- `docs/API_REFERENCE.md` — all **77 ops / 70 paths / 11 tags**, spec-facts verified (3.0.1, no servers, global bearer, 0 operationIds, 200-only no-schema responses, `Page`/`Limit` vs `page`/`limit` casing). Envelope + middleware mapping verified against `ApiResponse.cs` + `GlobalExceptionMiddleware.cs`. 409 honestly marked not-emitted.
- `docs/SDK_WEB.md` — real hook/model/zod names; **two corrections caught by spot-checks**: (1) body-writes are `useQuery`-style overloads, param-GETs are `useMutation` (`{params}` variables) — recipes fixed; (2) all responses typed `void` (no spec schemas) — envelope-cast pattern documented.
- `docs/SDK_MOBILE.md` — pubspec/factory/script quoted; Dart signatures spot-checked (`apiPostsFeedGet({page, limit})`, `apiPostsPost({required createPostRequest})`, all-optional `CreatePostRequest()` ctor).
- `docs/TOOLING_AND_PIPELINE.md` — isolation layout, build-before-generate order, CLI ref, CI example (marked deferred wiring).
- `README.md` — rewritten as portal; legacy lowercase docs explicitly marked superseded.

## Edge cases / honesty notes
- `usePostApiUserSignIn` mentioned only as token source (not a full recipe) — avoids repeating the query-style caveat.
- Mobile upload recipe stays at `MultipartFile.fromFile` level (no invented field names — media endpoints take model metadata).
- CI workflow is an example snippet, not a committed workflow (wiring still deferred per ADR-012/013).

## Verification
- `Test-Path` green for all 5 docs + README + every source path referenced (models, validations, factory, script, pubspec, spec, middleware).
- Endpoint count reconciled per-section: 8+7+2+7+4+13+14+8+1+7+6 = 77. No TODO stubs.
- No .NET/TS/Dart code touched; no tests needed (docs-only). Plans budget: 4 files under `plans/enterprise-docs/` (≤ 8).

## Follow-ups
- Regenerate docs when: new endpoints (API_REFERENCE), index/invariant changes (ARCHITECTURE), generator bumps (SDK_* / TOOLING).
- Still deferred: operationIds, Scalar UI, CI wiring (docs already describe the target state).
