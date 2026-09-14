# Feature: Clean Architecture Refactor & Codebase Reorganization

## Goal
Restore clean architectural boundaries across all layers in DotNetCoreSocialApi (.NET 9), eliminate illegal dependencies, remove duplicate and orphaned files, standardize naming/patterns, and ensure the entire solution builds cleanly.

## Acceptance Criteria
- [x] Solution `Social.sln` builds without any errors via `dotnet build`.
- [ ] `Social.Core` has ZERO references to EF Core or ASP.NET runtime.
- [ ] `Social.Application` references ONLY `Social.Core`.
- [ ] `Social.Infrastructure` references ONLY `Social.Core`.
- [ ] `Social.API` references `Social.Application`, `Social.Infrastructure`, and `Social.Core`.
- [ ] All duplicate/misspelled files (`Commends/`, `GenralConfig.cs`, duplicate `ICacheService.cs`, empty `ITokenService.cs`) deleted.
- [ ] Centralized `GlobalExceptionMiddleware` added to API pipeline.
- [ ] Redundant `GetUserId()` unified in `BaseController`.
- [ ] Caching services relocated to Infrastructure with single interface in Core.

## Scope
- In-scope: Project references, solution file, entity cleanup, DTO/service relocation, namespace fixes, duplicate deletion, exception middleware.
- Out-of-scope: Database schema rewriting or breaking existing database migration history.

## Estimated Complexity
L (Large refactoring across all 4 solution layers).
