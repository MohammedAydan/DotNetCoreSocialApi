# Feature Review: Test Suites & Codebase Improvements

## What Was Built
1. **Automated Test Architecture (`Social.Tests`)**:
   - Built a dedicated test project on .NET 9 with `xUnit`, `FluentAssertions`, `NSubstitute`, and `Microsoft.AspNetCore.Mvc.Testing`.
   - Integrated `Social.Tests` cleanly into `Social.sln`.
   - Added `TestAuthHandler` providing simulated authentication with configurable claims (`NameIdentifier`, `Email`, `Role`, `X-Anonymous`).
   - Added `CustomWebApplicationFactory` for full integration testing of API endpoints with in-memory doubles and disabled external dependencies.

2. **Comprehensive Unit Test Coverage**:
   - `UsersCommandTests` & `UsersQueryTests`: Complete test coverage for `CreateUserCommand`, `SignInCommand`, `RefreshTokenCommand`, `ChangePasswordCommand`, `ForgetPasswordCommand`, `ResetPasswordCommand`, `LogoutCommand`, `GetUserByIdQuery`, and `SearchUsersQuery`.
   - `PostsCommandAndQueryTests`: Coverage for `AddPostCommand`, `SharePostCommand`, `UpdatePostCommand`, `DeletePostCommand`, `GetFeedPostsQuery`, `GetMyPostsQuery`, `GetPostByIdQuery`, and `GetPostsByUserIdQuery`.
   - `CommentsCommandAndQueryTests`: Coverage for `AddCommentCommand`, `AddReplyCommentCommand`, `UpdateCommentCommand`, `DeleteCommentCommand`, `GetCommentsByPostIdQuery`, and `GetCommentByIdQuery`.
   - `SocialInteractionsCommandTests`: Coverage for Follow/Unfollow, Accept/Reject follow requests, Add/Remove likes, Block/Unblock users, and full Notification lifecycle.
   - `ValidationBehaviorTests`: Coverage for MediatR pipeline validation passing, failing, and handling commands with no validators.

3. **Full Integration Test Coverage Across All Controllers**:
   - `UserControllerTests`: Endpoints `/api/v1/user/register`, `/api/v1/user/sign-in`, `/api/v1/user/refresh-token`, `/api/v1/user/me`, `/api/v1/user/search`.
   - `PostsControllerTests`: Endpoints `/api/v1/posts` (POST, GET feed, GET my posts, GET by ID, DELETE).
   - `CommentsControllerTests`: Endpoints `/api/v1/comments` (POST comment, GET post comments, PUT update comment, DELETE comment).
   - `SocialInteractionsControllerTests`: Endpoints for follow, unfollow, get followers, add/remove like, get post likes, block user, get blocked users, create notification, get notification by ID, and mark as read.
   - `GlobalExceptionMiddlewareTests`: Verified standardized error envelopes and appropriate HTTP status codes (400, 401, 404, 500).

4. **FluentValidation Pipeline Integration**:
   - Added `FluentValidation.DependencyInjectionExtensions` 11.11.0.
   - Implemented `ValidationBehavior<TRequest, TResponse>` in `Social.Application/Behaviors/`.
   - Added `AddPostCommandValidator` and registered validators and pipeline behavior in MediatR container.
   - Updated `GlobalExceptionMiddleware` to intercept `FluentValidation.ValidationException` and return formatted 400 Bad Request responses.

5. **`CancellationToken` Propagation**:
   - Added `CancellationToken cancellationToken = default` across all repository interfaces:
     - `IUserRepository` & `UserRepository`
     - `IPostRepository` & `PostRepository`
     - `ICommentRepository` & `CommentRepository`
     - `IFollowRepository` & `FollowRepository`
     - `ILikeRepository` & `LikeRepository`
     - `IBlockUserRepository` & `BlockUserRepository`
     - `INotificationRepository` & `NotificationRepository`
   - Propagated tokens to all EF Core database operations (`FirstOrDefaultAsync`, `ToListAsync`, `AddAsync`, `SaveChangesAsync`, etc.).

---

## Edge Cases Handled
- **Constructor Deserialization in `AuthResponse`**: Added parameterless constructor and public `Token` property to avoid `System.Text.Json` reflection failures during client response parsing.
- **Null-Safety in `CreateUserCommandHandler`**: Guarded against null `UserGender` string when performing lowercase comparison.
- **Nullable Navigations in LINQ**: Applied null-forgiving operators on EF `ThenInclude` chains to prevent CS8602 compiler warnings.
- **Backward Compatibility**: All repository `CancellationToken` parameters have default values (`= default`), preventing breaking changes for any existing callers.
- **Test Rate Limiting**: Customized rate limit policies inside integration test factory to prevent rate limiter 429 throttling during rapid test execution.

---

## Known Limitations & Tech Debt
- Entities still hold `[NotMapped]` viewer session flags (`IsLiked`, `IsFollower`, `IsFollowing`). These should eventually be refactored entirely into application DTOs.
- `AutoMapper` 14.0.0 has a known upstream advisory (GHSA-rvv3-g6hj-g44x). Consider upgrading or replacing with Mapster or manual mapping in a future maintenance cycle.

---

## Verification Summary
- **Tests**: 74 / 74 Passed (100% success rate)
- **Compiler Errors**: 0
- **Compiler Warnings**: 0 (only 2 NuGet security advisories from third-party packages)
- **Configuration**: Verified in both Debug and Release configurations.
