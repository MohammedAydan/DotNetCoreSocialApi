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
