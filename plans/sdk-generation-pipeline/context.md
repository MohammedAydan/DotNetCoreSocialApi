# Context — sdk-generation-pipeline

## Files to create
- `package.json` (root, pnpm; scripts: openapi:export, generate:web, generate:mobile, generate:all)
- `orval.config.ts` (socialApi react-query + socialValidation zod)
- `tsconfig.json` (strict, for tsc verification of generated code)
- `src/api/custom-instance.ts` (Axios + Bearer interceptor + AbortController; NO CancelToken)
- `openapi.json` (exported spec, gitignored or committed as build artifact)
- `packages/social_api_client/lib/api_client_factory.dart` (hand-written factory)
- `plans/sdk-generation-pipeline/review.md` (Phase 4 only)

## Files to modify
- `Social/Social.API.csproj` (add OpenApiGenerateDocuments, OpenApiDocumentsDirectory, ApiDescription.Server)
- `Social/Program.cs` (MapOpenApi unconditional; SwaggerUI stays dev-only)
- `.gitignore` (node_modules, generated dirs decision, openapi.json if needed)
- `plans/TECH_STACK.md`, `plans/ARCH.md`, `plans/DECISIONS.md`, `plans/context.md`, `plans/SESSION_LOG.md` (same-turn living-doc updates)

## Generated (do not hand-edit)
- `src/api/generated/endpoints/**`, `src/api/generated/models/**`, `src/api/generated/validations/**`
- `packages/social_api_client/**` (except `api_client_factory.dart`)

## New deps (pnpm only, never npm/yarn)
- Dev: `orval@^8.21.0` (CVE-2026-72717 fix; registry latest 8.33.0 satisfies range)
- Dev: `@openapitools/openapi-generator-cli@^2.41.0` — npm wrapper versioning differs from the Java generator: wrapper 2.41.0 bundles generator **7.25.0** (verified via `pnpm dlx ... version`), which satisfies the mission's `>=7.22.0` dart-dio fix requirement. `^7.22.0` does not exist on npm (latest wrapper is 2.x).
- Runtime: `@tanstack/react-query@^5`, `axios`, `zod`, `@hookform/resolvers`
- Runtime: `@tanstack/react-query@^5`, `axios`, `zod`, `@hookform/resolvers`
- Dart (via generator + pub): `dio`, `json_serializable`, `build_runner`, `json_annotation`

## Env vars
- `NEXT_PUBLIC_API_URL` (fallback `http://localhost:5000`; local dev API is `http://localhost:5157`)
- No secrets invented; export script uses localhost URL only

## Open questions
- Commit generated code? Mission says NO (generate at build time, cache outputs). Default: gitignore generated dirs, keep `openapi.json` as local artifact.
- Dart `pubName`: `social_api_client` confirmed.
- API port for export: mission says 5000, local launchSettings uses 5157 — script tries 5000 then 5157, then build-time fallback.
- RESOLVED (task 3): Orval `@openapitools/openapi-generator-cli@^7.22.0` does not exist on npm — using wrapper `^2.41.0` (bundles generator 7.25.0, verified).
- RESOLVED (task 3): Orval 8.33 has no `input.validation` key (validation default-on) — removed from `orval.config.ts`.
- RESOLVED (task 3): .NET emits `"default": null` (Microsoft.OpenApi.Any.OpenApiNull) for optional query params → broke zod gen. Fixed at source via document transformer in `Social/Program.cs` (strip OpenApiNull defaults; null-guarded `operation.Parameters`). Verified 0 null-defaults in regenerated spec, `tsc --noEmit` clean, hook+zod probe import compiles.
