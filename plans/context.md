# Project: DotNetCoreSocialApi

## Purpose
A backend RESTful API for a social media platform built with ASP.NET Core 9 and Clean Architecture, featuring user authentication (JWT & Refresh tokens), user profile management, posts, media, comments, likes, follower relationships, user blocking, notifications, distributed Redis caching, and rate limiting.

## Current Status
- Active feature: none (enterprise-docs completed; docs/ portal + README live)
- Overall health: green
- Last updated: 2026-09-16 (doc-audit-agents: 100% parity proven, AGENTS.md compass, health green)

## Critical Constraints
- Framework: .NET 9 / C# 13
- Database: MySQL with Pomelo EF Core
- Caching: Redis with in-memory fallback
- Architectural Pattern: Clean Architecture (Domain/Core -> Application -> Infrastructure -> API)
- Zero framework dependencies in Core/Domain layer

## Active Features
- clean-architecture-refactor: [Completed] Refactored architecture, project references, namespaces, and middleware.
- test-suites-and-codebase-improvements: [Completed] 74 unit and integration tests passing across all endpoints and handlers; FluentValidation MediatR pipeline active; CancellationToken propagated.
- admin-dashboard: [Completed] Production-grade embedded admin dashboard under /admin, dedicated login portal at /admin/login with dual-delivery JWT authentication (header + HttpOnly cookie), comprehensive CQRS admin API, RBAC, content moderation, analytics, audit logging, dedicated Social.Admin.Web Blazor UI sub-project with modern components, and 54 E2E/auth tests (128/128 total tests passing).
- grant-admin-permissions: [Completed] Initialized IDatabaseSeeder & DatabaseSeeder, automated startup seeding for mohammedaydan12@gmail.com with Admin, Moderator, User roles, confirmed email, verified status, and login elevation (130/130 tests passing).
- dashboard-routes-and-fix: [Completed] Resolved EF Core set operation translation failure in moderation feed; generated AddAuditLogsTable migration with startup auto-migration and resilient repository fallback; implemented dedicated browser routes (/admin, /admin/users, /admin/moderation, /admin/audit-logs, /admin/diagnostics) with active sidebar routing, toast notifications, and role/password modals (138/138 tests passing).
- post-moderation-and-controls: [Completed] Overhauled post and media moderation in admin console with rich post card layout, full content, responsive image gallery with lightbox, native HTML5 video player, client-side filter and search bar, post inspection modal, granular controls (visibility toggle, audience visibility updates, permanent deletion), and automated in-app author moderation notifications with reasons (149/149 tests passing).
- ui-redesign-and-date-standardization: [Completed] Universal UTC date standardization across EF Core, MySQL ValueConverter, and ASP.NET Core JSON serialization with strict ISO 8601 'Z' suffix; elevated moderation interface with View Switcher (Stream, Grid, List), card content height clamping with gradient fade and expand toggle, code snippet formatting, RTL/Arabic support, media mosaics, quick stat pills bar, and formatted timestamps with hover tooltips (160/160 tests passing).
- ef-core-indexes-and-relationships: [Completed] Eliminated shadow foreign keys, restored single-column FK indexes and unique indexes, reconciled MySQL non-transactional DDL schema drift, generated safe conditional migration AddIndexes & TunePostFeedIndexes, verified 0 data loss, and updated production database with 185/185 tests passing.
- diagnose-feed-userid1: [Completed] Programmatically audited EF Core runtime model and query SQL (proven 0 shadow properties, UserId exists, UserId1 does NOT exist); identified MonsterASP IIS application root path mismatch (`/wwwroot/` vs `/`); deployed fresh binaries to `/wwwroot/`; resolved admin login 400 credential sync; verified live production API endpoints with HTTP 200 OK and 100% clean SQL; 187/187 tests passing.

## Known Issues / Tech Debt
- View/Session flags (`IsLiked`, `IsFollower`, `IsFollowing`) on Core entity models (`[NotMapped]`) to be moved to DTOs in future refactor.
- AutoMapper 14.0.0 package advisory (GHSA-rvv3-g6hj-g44x) to be evaluated for upgrade or alternative mapping library.

## Team / Ownership
- Backend: Mohammed Aydan
