# Plan: Test Suites & Codebase Improvements

## Goal Description
Establish a production-grade automated testing architecture for DotNetCoreSocialApi (.NET 9) covering all feature areas (Commands, Queries, Domain) and all REST API endpoints. Additionally, implement key architectural improvements identified in the audit: FluentValidation MediatR pipeline behaviors, `CancellationToken` propagation across repository contracts, and entity state decoupling.

---

## What Else Needs Improvement (Audit Findings)

### 1. Missing Automated Test Suite
- `Social.Tests` is currently empty. There is zero regression protection for authentication, authorization, social graphs, posts, or caching logic.

### 2. Ad-hoc Request Validation
- Validation is manually performed via `if (string.IsNullOrWhiteSpace(...))` scattered inside controllers and handlers.
- **Remediation**: Introduce **FluentValidation** + MediatR `ValidationBehavior<TRequest, TResponse>` pipeline behavior to automatically validate incoming commands/requests before reaching handlers.

### 3. Missing `CancellationToken` on Repositories
- Repository interfaces (`IUserRepository`, `IPostRepository`, `ICommentRepository`, `IFollowRepository`, etc.) do not accept `CancellationToken`.
- If an HTTP request is cancelled or times out, database operations continue executing needlessly.
- **Remediation**: Add `CancellationToken cancellationToken = default` to all repository contracts and EF Core async calls.

### 4. Leaky Domain State (`[NotMapped]` on Entities)
- `User.cs` has viewer-specific flags: `IsFollower`, `IsFollowing`, etc.
- `Post.cs` has viewer-specific flag: `IsLiked`.
- Placing viewer session context in persistent domain entities pollutes domain models and causes concurrency/caching complications.
- **Remediation**: Move viewer flags exclusively to Application DTOs (`UserDto`, `PostDto`).

---

## User Review Required

- Testing stack: **xUnit**, **FluentAssertions**, **NSubstitute**, **`Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`)**.
- Scope: Unit tests for all CQRS command and query handlers across all 7 feature areas + Integration tests for all 8 API controllers.

---

## Verification Plan
- Build test project: `dotnet build Social.Tests/Social.Tests.csproj`
- Run all tests: `dotnet test Social.sln --logger "console;verbosity=detailed"`
