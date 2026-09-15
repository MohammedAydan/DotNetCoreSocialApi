# Engineering Patterns

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
