# Architecture Decisions

## ADR-001: Separation of Identity Abstraction from EF Core in Core Layer
- **Date:** 2026-09-14
- **Status:** Accepted
- **Context:** `Social.Core.csproj` referenced `Microsoft.AspNetCore.Identity.EntityFrameworkCore` and `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, dragging relational database and ASP.NET runtime dependencies into the pure domain layer.
- **Decision:** Replace EF Core Identity in Core with `Microsoft.Extensions.Identity.Stores` (and remove the web framework reference). `User : IdentityUser` continues to function without polluting Core with Entity Framework or ASP.NET hosting dependencies.
- **Alternatives considered:**
  1. Complete POCO conversion with separate `ApplicationUser` in Infrastructure. Evaluated as unnecessarily disruptive to existing repository interfaces and commands.
- **Consequences:** Core remains clean of EF Core, while existing domain contracts relying on `IdentityUser` remain stable.

## ADR-002: Removal of Application Layer Reference to Infrastructure
- **Date:** 2026-09-14
- **Status:** Accepted
- **Context:** `Social.Application.csproj` contained `<ProjectReference Include="..\Social.Infrastructure\Social.Infrastructure.csproj" />`, directly violating the Clean Architecture dependency rule.
- **Decision:** Remove the reference. Ensure all Application handlers use `Social.Core.Interfaces` (e.g. `ITokenService`, `ICacheService`).
- **Consequences:** Strict compile-time enforcement that Application cannot access Infrastructure internals.

## ADR-003: Consolidation of ICacheService and Infrastructure Placement
- **Date:** 2026-09-14
- **Status:** Accepted
- **Context:** A duplicate `ICacheService` existed in `Social.API.Services.Caching`, shadowing `Social.Core.Interfaces.ICacheService`, and implementation classes were located in the presentation project.
- **Decision:** Keep only `Social.Core.Interfaces.ICacheService`. Move implementations (`RedisCacheService`, `InMemoryCacheService`) to `Social.Infrastructure/Caching/`.
- **Consequences:** All layers consume cache abstraction via Core interface; concrete caching infrastructure resides in Infrastructure.

## ADR-004: Dedicated Razor Class Library (Social.Admin.Web) for Admin Dashboard UI Sub-Project
- **Date:** 2026-09-15
- **Status:** Accepted
- **Context:** The initial Admin UI was embedded as an inline HTML string inside an API controller, lacking modularity, testability, and enterprise architecture. The user requested a clean, distributed, dedicated UI subproject using Blazor/Razor components.
- **Decision:** Architect `Social.Admin.Web` as a standalone Razor Class Library (`Microsoft.NET.Sdk.Razor`) in the solution, targeting `net9.0`. It encapsulates all dashboard models, `IAdminDashboardService` (orchestrating MediatR CQRS commands/queries), layout components (`AdminLayout`, `AdminSidebar`, `AdminNavbar`), page components (`OverviewDashboard`, `UserManagement`, `ContentModeration`, `AuditLogViewer`, `SystemDiagnostics`), interactive modals, and static web assets (`admin-dashboard.css`, `admin-dashboard.js`). `Social.API` references `Social.Admin.Web` and serves it under `/admin` with strict `[Authorize(Roles = "Admin")]` enforcement.
- **Alternatives considered:**
  1. Monolithic Razor Pages inside `Social.API`: Pollutes the API presentation layer with UI views and assets.
  2. Standalone SPA (React/Vue): Adds node/npm toolchain, separate hosting/CORS configuration, and complicates single-binary deployment.
- **Consequences:** Clean separation of UI concerns, full reuse of Blazor/Razor component patterns, direct compilation in `Social.sln`, and 100% preservation of all existing REST API endpoints and 121 automated tests.
## ADR-005: Dual-Delivery JWT Authentication for Browser and API Clients with Dedicated Admin Login Route (/admin/login)
- **Date:** 2026-09-15
- **Status:** Accepted
- **Context:** Standard ASP.NET Core JWT Bearer authentication sends an empty HTTP 401 challenge header. While REST API clients handle 401 and supply an `Authorization: Bearer <token>` header, web browsers navigating to `http://localhost:5157/admin` cannot prompt for bearer tokens, resulting in a blank browser "HTTP ERROR 401" screen.
- **Decision:**
  1. Implement an unauthenticated redirect from `/admin` to `/admin/login` (HTTP 302) when accessed without credentials, directing users to a responsive, dark-mode Administrator Login page.
  2. Implement `POST /admin/login` which authenticates credentials using `SignInCommand`, strictly validates administrator role privileges (`result.IsAdmin()`), and issues an `admin_token` cookie configured with `HttpOnly`, `SameSite=Lax`, and 7-day expiration.
  3. Configure `JwtBearerEvents.OnMessageReceived` in `Social.Infrastructure/DependencyInjection.cs` to inspect `context.Request.Cookies["admin_token"]` whenever no `Authorization` header is present.
  4. Extend `TokenBlacklistMiddleware` to verify both `Authorization` header and `admin_token` cookie against revocation.
  5. Provide `/admin/logout` endpoint to clear the cookie and redirect back to `/admin/login`.
- **Alternatives considered:**
  1. Traditional ASP.NET Core CookieAuthentication alongside JwtBearer: Adds dual authentication schemes and complexity with scheme switching and challenge redirects across API and Web routes.
  2. Basic Authentication: Insecure, non-standard for token-based microservices, and does not leverage existing JWT claims and token revocation infrastructure.
- **Consequences:** Seamless browser experience for administrators with full login/logout flows; standard REST API clients continue using `Authorization: Bearer ...` header unmodified; zero regressions across all 128 automated integration and unit tests.
