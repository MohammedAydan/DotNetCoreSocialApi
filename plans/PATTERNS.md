# Engineering Patterns

## Pattern: Standardized API Response Envelope [feature: clean-architecture-refactor]
- **Problem:** API returns mixed error/success formats, inconsistent status envelopes.
- **Solution:** Use `ApiResponse<T>` with standard factories `SuccessResponse(message, data)` and `ErrorResponse(message, errors)`.
- **Location:** `Social.Application.Common.ApiResponse` / `Social.API.Controllers.BaseController`.

## Pattern: Global Exception Handling Middleware [feature: clean-architecture-refactor]
- **Problem:** Controllers repeat `try { ... } catch (Exception ex) { return ApiServerError(...); }` across dozens of methods.
- **Solution:** Centralized `GlobalExceptionMiddleware` mapping known exception types (`InvalidOperationException` -> 400, `KeyNotFoundException` -> 404, `UnauthorizedAccessException` -> 401, `Exception` -> 500) into `ApiResponse<object>`.
- **Location:** `Social.API.Middlewares.GlobalExceptionMiddleware`.
