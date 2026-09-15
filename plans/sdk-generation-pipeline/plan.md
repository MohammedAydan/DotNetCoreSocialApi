# Plan — sdk-generation-pipeline

## Goal
Build an automated, type-safe, contract-driven SDK generation workflow from the .NET 9 OpenAPI spec for Web (TypeScript/Next.js via Orval) and Mobile (Flutter/Dart via openapi-generator `dart-dio`), using `pnpm` exclusively, with AbortController-based Axios client (no deprecated CancelToken).

## Acceptance criteria (testable)
1. `pnpm run openapi:export` produces valid `openapi.json` at repo root (from live API `GET /openapi/v1.json` or build-time `OpenApiGenerateDocuments` output).
2. `pnpm run generate:web` produces `src/api/generated/{endpoints,models,validations}` with React Query v5 hooks + Zod schemas; `pnpm exec tsc --noEmit` passes with zero errors.
3. Sample imports resolve: Zod schema import and `useGetPosts`-style hook import compile.
4. `pnpm run generate:mobile` produces `packages/social_api_client` Dart package (`lib/api`, `lib/model`, pubspec) with `json_serializable` + Dio; `dart run build_runner build` emits `.g.dart`; `flutter analyze` passes with 0 errors.
5. `SocialApiClientFactory.create(baseUrl:getToken:)` compiles and injects `Authorization: Bearer` header.
6. `pnpm run generate:all` chains export → web → mobile in one command.
7. `dotnet build Social.sln -c Release` still 0 errors; no secrets committed.

## Approach
1. .NET prerequisite: add `OpenApiGenerateDocuments=true` + `Microsoft.Extensions.ApiDescription.Server` to `Social/Social.API.csproj`; make `app.MapOpenApi()` unconditional (keep SwaggerUI dev-only) so `curl /openapi/v1.json` works in any env.
2. JS workspace at repo root: `package.json` (pnpm), `orval.config.ts` (two configs: `socialApi` react-query + `socialValidation` zod), `src/api/custom-instance.ts` (Axios + Bearer interceptor + AbortController, NO CancelToken), `tsconfig.json`.
3. Export live spec → run Orval → verify with `tsc --noEmit`.
4. Generate Dart package via `pnpm dlx @openapitools/openapi-generator-cli` (`dart-dio`, `json_serializable`, null-safe), add hand-written `api_client_factory.dart`, run `flutter pub get` + `build_runner`, verify with `flutter analyze`.
5. Wire `package.json` scripts (`openapi:export`, `generate:web`, `generate:mobile`, `generate:all`); update living docs + SESSION_LOG; write `review.md` on close.

## Scope
IN:
- .NET csproj/Program.cs OpenAPI export readiness
- Root `package.json`, `orval.config.ts`, `tsconfig.json`, `src/api/custom-instance.ts`
- Orval web generation + tsc verification
- Dart package generation + factory + build_runner + flutter analyze
- pnpm scripts + docs
OUT:
- No Next.js app pages, no React Hook Form demo app (snippets only)
- No Scalar UI, no CI workflow file, no pub publish, no prod migration
- No changes to business logic, controllers, or auth semantics

## Dependencies
- Toolchain verified: .NET 9.0.310, Node v25.9.0, pnpm 10.33.2, Flutter 3.47.0 / Dart 3.13.0, Java 23 (for generator)
- Runtime needs API running locally for `openapi:export` (curl); fallback is build-time JSON
- Network needed for `pnpm add` (registry) and Flutter `pub get`

## Complexity
M (multi-tool orchestration, but scoped; no business-logic changes)
