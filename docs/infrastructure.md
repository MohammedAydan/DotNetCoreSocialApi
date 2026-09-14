# ⚙️ Infrastructure & Data

## Database Configuration
- **Provider**: MySQL (using `Pomelo.EntityFrameworkCore.MySql`).
- **Context**: `ApplicationDbContext` manages the data flow, implementing `IdentityDbContext`.
- **Naming Strategy**: Uses default EF Core naming conventions.

## 💾 Caching Strategy
- **Service**: `ICacheService` (implemented via Redis).
- **Behavior**:
  - **Cache-Aside Pattern**: Commands invalidate keys, while queries populate them.
  - **Fallback**: Automatically falls back to in-memory caching if Redis connection fails.
- **Key Examples**:
  - `user:profile:{userId}`: Invalidated on user update.
  - `post:{postId}`: Invalidated on post update or comment addition.

## 📦 Repository Pattern
- **Generic Approach**: While standard EF Core is used, each domain entity has a dedicated Repository interface in `Social.Core.Interfaces`.
- **Examples**:
  - `IPostRepository`: Custom logic for feed generation and sharing.
  - `IUserRepository`: Integration with Identity `UserManager` and social relationship logic.

## 🚦 Rate Limiting
- **Implementation**: `AspNetCoreRateLimit`.
- **Config**: Defined in `appsettings.json` under `IpRateLimiting`.
- **Storage**: Policy and counter stores are registered in `Program.cs`.
