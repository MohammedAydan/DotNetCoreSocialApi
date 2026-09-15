# Engineering Patterns

## Pattern: Verify-Before-Delete on Filesystem Moves [feature: sdk-isolation]
- **Problem:** Chained `Move-Item ...; Remove-Item <source-parent>` deleted an SDK tree when the move failed on a transient OS file lock.
- **Solution:** Never chain a destructive command after a move. Check the move result (exit code / destination exists) before deleting anything; prefer copy → verify → delete, or regenerate-from-source when the tree is disposable.
- **Gotchas:** Windows file locks (indexers, watchers, just-exited daemons) make directory moves flaky; `Get-Process` may already show nothing by the time you check.

## Pattern: Contract-Driven SDK Generation (fix the spec, not the output) [feature: sdk-generation-pipeline]
- **Problem:** Generators emit un-compilable code (Orval zod `.default(null)`, `build_runner` format failures) from a technically-valid OpenAPI document.
- **Solution:** Fix the contract at the source (document transformer strips `OpenApiNull` defaults) and codify unavoidable generator quirks as post-generation steps in versioned scripts (`generate-mobile.mjs`: pubspec `^3.8.0` patch, asset restore). Never hand-edit generated trees; keep hand-written sources outside them (`sdk-assets/`, `src/api/custom-instance.ts`) and restore via script.
- **Gotchas:** Verify fixes by re-probing the emitted spec + full from-zero `generate:all`, not by reading generator logs. Microsoft.OpenApi 1.x models `Default` as `IOpenApiAny` (`OpenApiNull`), not `JsonNode` — type-checks against the wrong model fail silently.

## Pattern: Standardized API Response Envelope [feature: clean-architecture-refactor]
- **Problem:** API returns mixed error/success formats, inconsistent status envelopes.
- **Solution:** Use `ApiResponse<T>` with standard factories `SuccessResponse(message, data)` and `ErrorResponse(message, errors)`.
- **Location:** `Social.Application.Common.ApiResponse` / `Social.API.Controllers.BaseController`.

## Pattern: Global Exception Handling Middleware [feature: clean-architecture-refactor]
- **Problem:** Controllers repeat `try { ... } catch (Exception ex) { return ApiServerError(...); }` across dozens of methods.
- **Solution:** Centralized `GlobalExceptionMiddleware` mapping known exception types (`InvalidOperationException` -> 400, `KeyNotFoundException` -> 404, `UnauthorizedAccessException` -> 401, `Exception` -> 500) into `ApiResponse<object>`.
- **Location:** `Social.API.Middlewares.GlobalExceptionMiddleware`.
## Pattern: Atomic Counter via ExecuteUpdate + Rows Guard [feature: refactor-social-feed-and-indexes]
- **Problem:** Read-modify-write counters (`PostsCount++`) race under concurrency and orphan rows when the parent insert succeeds but the user is missing.
- **Solution:** `await using var tx = await _context.Database.BeginTransactionAsync(ct);` + `ExecuteUpdateAsync(SetProperty(...))`; if rows==0 throw `KeyNotFoundException` before `CommitAsync`; `DisposeAsync` auto-rolls back.
- **Gotchas:** EF InMemory provider does not support `ExecuteUpdate`/transactions — use SQLite in-memory relational tests.
## Pattern: Batch Likes with Parent-Chain HashSet [feature: refactor-social-feed-and-indexes]
- **Problem:** N+1 likes checks per post/parent.
- **Solution:** Collect post + 3-level `ParentPost` ids into `HashSet<string>`, one `WHERE PostId IN (...)` query, map `IsLiked` in O(1) recursively.
