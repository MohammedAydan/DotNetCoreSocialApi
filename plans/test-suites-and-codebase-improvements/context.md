# Context: Test Suites & Codebase Improvements

## Files to Create
- `Social.Tests/Social.Tests.csproj`
- `Social.Tests/Infrastructure/CustomWebApplicationFactory.cs`
- `Social.Tests/Infrastructure/TestAuthHandler.cs`
- `Social.Tests/Unit/Features/*/*.cs`
- `Social.Tests/Integration/Controllers/*.cs`

## Files to Modify
- `Social.sln`
- `Social.Application/DependencyInjection.cs` (if adding validation behavior)
- `Social.Core/Interfaces/*.cs` (for CancellationToken support)

## Dependencies to Add
- `Microsoft.NET.Test.Sdk`
- `xunit`
- `xunit.runner.visualstudio`
- `FluentAssertions`
- `NSubstitute`
- `Microsoft.AspNetCore.Mvc.Testing`
