# Tech Stack

## Runtime
| Layer | Technology | Version | Notes |
|-------|-----------|---------|-------|
| Language | C# | 13 | Nullable enabled, ImplicitUsings enabled |
| Runtime | .NET | 9.0 | SDK 9.0.310 |
| Solution | Visual Studio Solution | 12.00 / VS 2022 | `Social.sln` |

## Presentation Layer (Social.API)
| Layer | Technology | Version | Notes |
|-------|-----------|---------|-------|
| Framework | ASP.NET Core Web API | 9.0.4 | Minimal Hosting / Controllers |
| OpenAPI / Docs | Microsoft.AspNetCore.OpenApi | 9.0.4 | Swashbuckle UI 8.1.1 |
| Rate Limiting | AspNetCoreRateLimit | 5.0.0 | IP-based client rate limiting |
| Env Loader | DotNetEnv | 3.1.1 | `.env` file loader |

## Admin UI Sub-Project (Social.Admin.Web)
| Layer | Technology | Version | Notes |
|-------|-----------|---------|-------|
| UI Framework | Razor Class Library / Blazor Components | 9.0.2 | `Microsoft.NET.Sdk.Razor` |
| Component Abstractions | Microsoft.AspNetCore.Components.Web | 9.0.2 | Blazor component model |
| Authorization | Microsoft.AspNetCore.Components.Authorization | 9.0.2 | Component-level RBAC |
| Assets | Native CSS / JS | Modern Slate Theme | Dark-mode design system with responsive layouts |

## Application Layer (Social.Application)
| Layer | Technology | Version | Notes |
|-------|-----------|---------|-------|
| Mediator | MediatR | 12.5.0 | CQRS pipeline |
| Mapping | AutoMapper | 14.0.0 | DTO mapping profiles |
| Validation | FluentValidation.DependencyInjectionExtensions | 11.11.0 | MediatR request validation pipeline behavior |

## Infrastructure Layer (Social.Infrastructure)
| Layer | Technology | Version | Notes |
|-------|-----------|---------|-------|
| ORM | EF Core | 9.0.4 | Code-First migrations |
| Database Driver | Pomelo.EntityFrameworkCore.MySql | 9.0.0 | MySQL 8.0+ |
| Identity Store | Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.4 | ASP.NET Identity persistence |
| Auth Tokens | Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.4 | JWT validation |
| Distributed Cache | StackExchange.Redis | 2.8.16 | Redis caching with fallback |

## Domain Layer (Social.Core)
| Layer | Technology | Version | Notes |
|-------|-----------|---------|-------|
| Identity Abstraction | Microsoft.Extensions.Identity.Stores | 9.0.4 | Lightweight abstractions without EF Core |

## Test Tooling (Social.Tests)
| Tool | Technology | Version | Notes |
|------|-----------|---------|-------|
| Test Framework | xUnit | 2.9.3 | Unit and integration test runner |
| Test SDK | Microsoft.NET.Test.Sdk | 17.13.0 | Visual Studio / CLI test engine |
| Test Runner | xunit.runner.visualstudio | 3.0.2 | Visual Studio test adapter |
| Assertion Library | FluentAssertions | 7.2.0 | Fluent, readable assertions |
| Mocking Library | NSubstitute | 5.3.0 | Modern mock library for .NET |
| Web Test Host | Microsoft.AspNetCore.Mvc.Testing | 9.0.2 | In-memory WebApplicationFactory |
| EF Test Provider | Microsoft.EntityFrameworkCore.InMemory | 9.0.4 | Non-relational probe only; ExecuteUpdate unsupported |
| Relational Test DB | Microsoft.EntityFrameworkCore.Sqlite | 9.0.4 | SQLite in-memory for ExecuteUpdate/transaction repo tests |
