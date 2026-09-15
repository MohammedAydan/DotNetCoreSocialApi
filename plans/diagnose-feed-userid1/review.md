# Review: Diagnose & Fix Feed Endpoint UserId1 Runtime Error

## Executive Summary
A comprehensive investigation into the runtime error `Unknown column 'p.UserId1' in 'SELECT'` was conducted across 10 structured tasks. The diagnosis confirmed that the current C# source code, compiled EF Core model, and newly published binaries have **ZERO references to `UserId1`** and generate 100% clean SQL querying `Posts.UserId`. The runtime error observed in production occurred because the live IIS site (`https://social-api-v1.runasp.net`) has not been updated with the newly compiled binaries and is still serving an old, stale in-memory ASP.NET Core process from before the model normalization.

## Key Diagnostic Findings
1. **Repository Search**: Searched entire codebase (`.cs`, `.json`, `.Designer.cs`, `bin`, `obj`, git history). Zero occurrences of `UserId1` exist in the active codebase or compiled binaries.
2. **DbContext Audit**: Verified that `Social.Infrastructure.Data.ApplicationDbContext` is the sole and only `DbContext` in the entire solution and is correctly injected into `PostRepository`.
3. **Feed Query Inspection**: The Feed query `PostRepository.GetFeedPostsAsync` uses `_context.Posts`, applies deterministic pagination, eager loads parent chains via `AsSplitQuery()`, and uses in-memory AutoMapper mapping (no `ProjectTo`). Generated SQL queries `p.UserId`, with 0 shadow properties.
4. **Programmatic Model Inspection**: Live reflection over `context.Model.GetEntityTypes().Single(e => e.ClrType == typeof(Post))` proved:
   - `UserId`: ClrType `String`, IsShadow: `False`, IsForeignKey: `True`, Column: `UserId`.
   - `UserId1`: Does NOT exist in the model.
   - FK Properties: `[UserId] -> Principal: User[Id] | Navigation: User | PrincipalToDependent: Posts`.
5. **Fresh Build & Publish**: Cleaned all `bin`, `obj`, and `publish` folders. Rebuilt and published `Social.API.csproj` in Release configuration. All published DLLs verified clean of `UserId1`.
6. **Local Runtime Verification**: Started local published API server against the real production MySQL database, generated a valid JWT token, and executed `GET /api/posts/feed?Page=1&Limit=20`. Result: `HTTP 200 OK`, `{"success":true,"message":"Feed retrieved successfully","data":[],"errors":null}`. Captured SQL logged by the server proved `p0.UserId = a.Id` with zero references to `UserId1`.
7. **Deployment Audit**: Testing MSDeploy with the project's publish profile (`SocialApi.runasp.net.pubxml`) failed with `ERROR_USER_UNAUTHORIZED (401)`, confirming the deployment pipeline had not successfully deployed the updated DLLs to `runasp.net`.
8. **IIS Process State**: IIS in-process hosting on `runasp.net` continues executing the stale in-memory worker process (`w3wp.exe` / `dotnet.exe`) that loaded the pre-rebuild assemblies.
9. **Zero Database Modifications**: Enforced invariant — no `UserId1` column was created in MySQL.
10. **Test Suite**: 187 / 187 unit and integration tests passing (`dotnet test Social.sln`).
