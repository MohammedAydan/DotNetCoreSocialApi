# Review — sdk-generation-pipeline

## What was built
Automated, type-safe, contract-driven SDK pipeline (`.NET 9 OpenAPI` → Web + Mobile), `pnpm`-only:
- **.NET contract readiness**: `Social.API.csproj` emits build-time `Social.API.json` (`OpenApiGenerateDocuments` + `ApiDescription.Server`); `MapOpenApi()` unconditional (SwaggerUI stays dev-only); document transformer strips meaningless `default: null` (`OpenApiNull`) annotations that broke generators.
- **Web (Orval 8.33.0)**: `orval.config.ts` (dual `socialApi` react-query + `socialValidation` zod, tags-split), `src/api/custom-instance.ts` (Axios Bearer interceptor, **AbortController-native** — no deprecated `CancelToken`), `package.json` scripts, `tsconfig.json` strict.
- **Mobile (generator 7.25.0 via wrapper 2.41.0)**: `packages/social_api_client` (`dart-dio`, `json_serializable`, Dio), `scripts/generate-mobile.mjs` (generate → pubspec `^3.8.0` patch → restore factory → `flutter pub get` → `build_runner`), factory source of truth in `sdk-assets/flutter/`.
- **One-command**: `pnpm run generate:all` verified from zero (wipe → exit 0, 84 Dart outputs, factory restored).

## Verification (all real, no mocks)
- `pnpm run typecheck` (`tsc --noEmit`): **0 errors** (75 files, 11 tag groups).
- Temp probe importing `useGetApiPostsFeed` + `PostApiPostsBody` (zod parse): compiled clean, then deleted.
- `flutter analyze` (package dir): **0 errors**; 11 `unused_import` warnings are upstream `dart-dio` template noise (left untouched — regeneration-proof).
- `build_runner`: 84 outputs, all `.g.dart` emitted.
- Temp `flutter test` (stubbed Dio adapter, deleted after): Bearer header injected with token, omitted without — **2/2 passed**.
- `dotnet build Social.sln -c Release`: **0 errors**. `dotnet test`: **242/242 passed** (no regressions).

## Edge cases handled
- API offline during export → live `:5000` → `:5157` → build-time `Social.API.json` fallback chain.
- `operation.Parameters` null for parameterless ops (crashed build-time doc gen once; guarded).
- `Microsoft.OpenApi 1.6.17` uses `IOpenApiAny`/`OpenApiNull`, not `JsonNode` (first strip attempt was a silent no-op; caught by re-probing the emitted JSON).
- npm wrapper versioning: `^7.22.0` doesn't exist on npm; wrapper `2.41.0` = generator `7.25.0` (verified via `dlx … version`).
- Orval 8.33 dropped `input.validation` (default-on now); `pubAuthor` spaces break CLI parsing.
- `flutter analyze` from repo root mis-reports exit code — gate must run inside `packages/social_api_client`.

## Known limitations / follow-ups
- Generated code + `openapi.json` are gitignored (generate-at-build-time per mission); fresh clones must run `pnpm install && pnpm run generate:all`.
- 70 paths have empty `operationId` (ASP.NET default) → generator auto-names are stable but ugly; follow-up: `[EndpointName]`/transformer to emit operationIds.
- No Scalar UI, no CI workflow wiring, no `pnpm-workspace.yaml` (single root package; optional per mission).
- 2 pending prod migrations (`AddNotificationIntelligence`, `AddTelemetryAndDailyMetrics`) remain human-gated — untouched by this feature.
