# Project: DotNetCoreSocialApi

## Purpose
A backend RESTful API for a social media platform built with ASP.NET Core 9 and Clean Architecture, featuring user authentication (JWT & Refresh tokens), user profile management, posts, media, comments, likes, follower relationships, user blocking, notifications, distributed Redis caching, and rate limiting.

## Current Status
- Active feature: none (all planned features complete)
- Overall health: green
- Last updated: 2026-09-14

## Critical Constraints
- Framework: .NET 9 / C# 13
- Database: MySQL with Pomelo EF Core
- Caching: Redis with in-memory fallback
- Architectural Pattern: Clean Architecture (Domain/Core -> Application -> Infrastructure -> API)
- Zero framework dependencies in Core/Domain layer

## Active Features
- clean-architecture-refactor: [Completed] Refactored architecture, project references, namespaces, and middleware.
- test-suites-and-codebase-improvements: [Completed] 74 unit and integration tests passing across all endpoints and handlers; FluentValidation MediatR pipeline active; CancellationToken propagated.

## Known Issues / Tech Debt
- View/Session flags (`IsLiked`, `IsFollower`, `IsFollowing`) on Core entity models (`[NotMapped]`) to be moved to DTOs in future refactor.
- AutoMapper 14.0.0 package advisory (GHSA-rvv3-g6hj-g44x) to be evaluated for upgrade or alternative mapping library.

## Team / Ownership
- Backend: Mohammed Aydan
