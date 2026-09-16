# Project: DotNetCoreSocialApi

## Purpose
A backend RESTful API for a social media platform built with ASP.NET Core 9 and Clean Architecture, featuring user authentication (JWT & Refresh tokens), user profile management, posts, media, comments, likes, follower relationships, user blocking, notifications, distributed Redis caching, and rate limiting.

## Current Status
- Active feature: none (meta-ui-rebuild completed; 270/270 tests)
- Overall health: green
- Last updated: 2026-09-17 (meta-ui-rebuild: Meta/Facebook-system console, light-first + FB dark mode, build 0 errors, 270/270 tests)

## Critical Constraints
- Framework: .NET 9 / C# 13
- Database: MySQL with Pomelo EF Core
- Caching: Redis with in-memory fallback
- Architectural Pattern: Clean Architecture (Domain/Core -> Application -> Infrastructure -> API)
- Zero framework dependencies in Core/Domain layer
- NEVER run `pnpm run generate:all` before `dotnet build Social.sln -c Release`
- NEVER physical-DELETE Posts rows; NEVER global HasQueryFilter(!IsDeleted); NEVER remove AsSplitQuery on post reads
- NEVER hand-edit generated trees (sdks/web/**, mobile lib/src/**)
- NEVER exceed HasMaxLength(255) on indexed string FKs; DateTime UTC + precision 6
- NEVER commit secrets/node_modules/artifacts; NEVER apply pending EF migrations without human approval (3 pending: telemetry, notification-intel, post-reports)

## Active Features
- admin-dashboard-overhaul: [Completed] World-class admin console redesign (elegant light/dark design system, polished shell) + in-dashboard documentation section at /admin/docs (270/270 tests)
- meta-ui-rebuild: [Completed] Meta/Facebook-system UI rebuild (light-first FB palette + FB dark mode, system typography, restructured sidebar/navbar, FB button system, borderless card layout, light login) across /admin/* + /admin/docs + /admin/login (270/270 tests)

## Known Issues / Tech Debt
- View/Session flags (`IsLiked`, `IsFollower`, `IsFollowing`) on Core entity models (`[NotMapped]`) to be moved to DTOs in future refactor.
- AutoMapper 14.0.0 package advisory (GHSA-rvv3-g6hj-g44x) to be evaluated for upgrade or alternative mapping library.
- Social.Admin.Web Blazor tree is dead code; live dashboard is string-HTML in AdminDashboardController.cs (182KB/2892 lines) + admin-dashboard.css (32KB) + admin-dashboard.js (2.7KB)
- 3 pending EF migrations NOT applied (need human approval)

## Team / Ownership
- Backend: Mohammed Aydan
