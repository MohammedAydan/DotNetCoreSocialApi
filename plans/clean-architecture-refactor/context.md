# Context: Clean Architecture Refactor

## Files to Modify
- `Social.sln`
- `Social.Core/Social.Core.csproj`
- `Social.Application/Social.Application.csproj`
- `Social/Social.API.csproj`
- `Social.Application/Features/Users/Commands/RefreshTokenCommand.cs`
- `Social.Core/Entities/RefreshToken.cs`
- `Social.Core/VisibilityValues.cs`
- `Social.Core/UserGenderTypes.cs`
- `Social/Controllers/BaseController.cs`
- `Social/Controllers/PostsController.cs`
- `Social/Controllers/UserController.cs`
- `Social/Controllers/BlockUserController.cs`
- `Social/Controllers/Dashboard/UserController.cs`
- `Social/Program.cs`
- `Social.Core/DependencyInjection.cs`
- `Social.Application/DependencyInjection.cs`
- `Social.Infrastructure/DependencyInjection.cs`

## Files to Delete
- `Social.Core/Entities/GenralConfig.cs`
- `Social.Application/Features/BlockUser/Commends/BlockUserCommend.cs`
- `Social.Application/Features/BlockUser/Commends/UnblockUserCommend.cs`
- `Social.Infrastructure/Token/ITokenService.cs`
- `Social/Services/Caching/ICacheService.cs`
- `api_test_errors.txt`

## Files to Create
- `Social/Middlewares/GlobalExceptionMiddleware.cs`
- `Social.Infrastructure/Caching/RedisCacheService.cs`
- `Social.Infrastructure/Caching/InMemoryCacheService.cs`
- `Social.Core/Configuration/EmailSettings.cs`
- `Social.Core/Configuration/GeneralConfig.cs`

## Dependencies Changed
- `Social.Core.csproj`: Remove `Microsoft.AspNetCore.App`, remove `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, add `Microsoft.Extensions.Identity.Stores` (9.0.4)
- `Social.Application.csproj`: Remove `Social.Infrastructure.csproj` project reference
- `Social/Social.API.csproj`: Add `Social.Infrastructure.csproj` project reference
