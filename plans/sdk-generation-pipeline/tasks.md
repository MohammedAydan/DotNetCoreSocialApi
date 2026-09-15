# Tasks — sdk-generation-pipeline

- [x] 1. .NET OpenAPI export readiness (csproj `OpenApiGenerateDocuments` + ApiDescription.Server; `MapOpenApi` unconditional)
- [x] 2. JS workspace bootstrap via pnpm (package.json, tsconfig, orval.config.ts, custom-instance with AbortController)
- [x] 3. Export `openapi.json` + run Orval web generation + `tsc --noEmit` verify
- [x] 4. Generate Dart `dart-dio` package + factory + build_runner + `flutter analyze` verify
- [x] 5. Wire `generate:all` scripts, update living docs, verify full pipeline, close with review.md
- [ ] 2. JS workspace bootstrap via pnpm (package.json, tsconfig, orval.config.ts, custom-instance with AbortController)
- [ ] 3. Export `openapi.json` + run Orval web generation + `tsc --noEmit` verify
- [ ] 4. Generate Dart `dart-dio` package + factory + build_runner + `flutter analyze` verify
- [ ] 5. Wire `generate:all` scripts, update living docs, verify full pipeline, close with review.md
