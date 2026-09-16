# DotNetCoreSocialApi

Backend RESTful API for a social media platform — **ASP.NET Core 9 / C# 13, Clean Architecture**, with JWT + refresh-token auth, posts / media / comments / likes, follow graph, user blocking, smart notifications, an admin console with enterprise analytics, Redis caching (in-memory fallback), and rate limiting. **242/242 tests passing.**

Type-safe clients are generated from the build-time OpenAPI contract — **TypeScript/React Query + Zod** for Web, **Dart/Dio** for Flutter — via an isolated `sdks/` pipeline (pnpm-only).

## Project structure

```
├── Social.sln                  # .NET solution
├── Social/                     # API host (controllers, middleware, Program.cs)
├── Social.Core/                # domain entities, interfaces, zero framework deps
├── Social.Application/         # CQRS (MediatR), validators, DTOs
├── Social.Infrastructure/      # EF Core 9 + MySQL, repositories, telemetry, caching
├── Social.Tests/               # 242 unit + integration tests
├── Social.Admin.Web/           # admin console UI (served under /admin)
├── sdks/
│   ├── generator/              # pipeline engine (@social/sdk-generator)
│   ├── web/                    # generated TS SDK (endpoints/ models/ validations/)
│   └── mobile/social_api_client/  # generated Dart SDK
├── docs/                       # enterprise documentation (start here 👇)
└── plans/                      # architecture records, session log, feature plans
```

## Quick start

```bash
# backend
dotnet build Social.sln -c Release
dotnet test Social.sln -c Release          # 242/242
dotnet run --project Social                # API on http://localhost:5157

# SDKs — build MUST run first (it emits Social/Social.API.json, the generator input)
cd sdks/generator
pnpm install
pnpm run generate:all                      # web + mobile
pnpm run typecheck                          # tsc --noEmit, 0 errors
# flutter analyze inside ../mobile/social_api_client → 0 errors
```

Live production: `https://social-api-v1.runasp.net` · Admin console: `/admin` (Admin role).

## Documentation portal

| Document | Contents |
|----------|----------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | System design, data flow, soft-delete & masking, privacy/sharing rules, two-way block, atomic counters, MySQL/EF indexing, split queries |
| [docs/API_REFERENCE.md](docs/API_REFERENCE.md) | Exhaustive endpoint catalog — all **83 operations / 76 paths** across 11 tags, with auth, params, bodies, responses, error matrix |
| [docs/SDK_WEB.md](docs/SDK_WEB.md) | Next.js/TS guide — Query v5 setup, AbortController Axios mutator, fetch/mutate/validate/error recipes |
| [docs/SDK_MOBILE.md](docs/SDK_MOBILE.md) | Flutter/Dart guide — path dependency, auth factory with secure storage, feed/post/upload recipes |
| [docs/TOOLING_AND_PIPELINE.md](docs/TOOLING_AND_PIPELINE.md) | Monorepo isolation, build-before-generate order, CLI reference, CI example |

Legacy per-area notes (`docs/api-endpoints.md`, `entities.md`, `infrastructure.md`, `security.md`, `project-analysis-report.md`) predate the SDK pipeline and analytics work — the five files above are authoritative. Note: `docs/architecture.md` was replaced in place by `ARCHITECTURE.md` (Windows filesystem is case-insensitive; the original is preserved in git history).

## Key invariants (see ARCHITECTURE.md)

- Soft delete only (`IsDeleted`); shares are never orphaned; deleted ancestors are masked for non-owners.
- Only `Public` posts from non-private accounts are shareable; blocks are enforced bidirectionally on every read and write.
- Counters move inside transactions via `ExecuteUpdateAsync` with zero-row guards; feed order is `CreatedAt DESC, Id DESC`, `limit` clamped to 50.
- Post reads: `Page`/`Limit` (capitalized); everything else: `page`/`limit`. All errors use `{ success, message, data, errors }`.
