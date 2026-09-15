# Context — sdk-isolation

## Repo-reality corrections to the mission brief (must-read)
- .NET API project is `Social/` (artifact `Social/Social.API.json`), NOT `src/Social.API/`. Tests are `Social.Tests/`, NOT `tests/`. All `../../src/Social.API/...` paths become `../../Social/...` from `sdks/generator/`.
- `orval.config.ts` is ESM (`"type": "module"`): mission's `__dirname` is undefined → use `import.meta.dirname` (Node 20.11+, we have Node 25).
- Mission Step 3 inlines the Java CLI in `package.json`; we keep `scripts/generate-mobile.mjs` (pubspec `^3.8.0` patch + factory restore are load-bearing — pipeline breaks without them, proven last session).
- Mission Step 4 ignore paths (`lib/api/`, `lib/model/`) do not match real `dart-dio` layout (`lib/src/api/`, `lib/src/model/`, `*.g.dart` next to models) — ignore block covers the real layout.
- Root `src/` is 100% SDK-owned (`src/api/...`); safe to delete after the move (verify contents first).
- `packages/` is fully covered by pre-existing NuGet `**/[Pp]ackages/*` ignore; new home `sdks/mobile/` needs explicit rules.
- `openapitools.json` (CLI version-manifest cache) regenerates CWD-relative → will live in `sdks/generator/`, ignored there.

## Files to move
- `package.json`, `pnpm-lock.yaml`, `orval.config.ts`, `tsconfig.json` → `sdks/generator/`
- `src/api/custom-instance.ts` → `sdks/generator/custom-instance.ts` (only non-generated file under root `src/`)
- `sdk-assets/` → `sdks/generator/sdk-assets/`
- `scripts/generate-mobile.mjs` → `sdks/generator/scripts/` (drop `export-openapi.mjs`: direct-artifact inputs make it dead code)
- `packages/social_api_client/` → `sdks/mobile/social_api_client/` (drop `.dart_tool/`, re-run pipeline)

## Files to delete
- Root: `node_modules/` (reinstall in new home), `src/`, `scripts/`, `sdk-assets/`, `packages/`, `openapi.json`, `openapitools.json`

## Files to modify
- `sdks/generator/orval.config.ts` (absolute input via `import.meta.dirname`, outputs `../web/...`, mutator `./custom-instance.ts`)
- `sdks/generator/tsconfig.json` (include config + custom-instance + `../web`)
- `sdks/generator/package.json` (name `@social/sdk-generator`, scripts repathed, no `openapi:export`)
- `sdks/generator/scripts/generate-mobile.mjs` (all paths repathed)
- `.gitignore` (replace old SDK block with `sdks/...` rules)
- Living docs on close: `TECH_STACK.md`, `ARCH.md`, `DECISIONS.md` (ADR-013), `context.md`, `SESSION_LOG.md`

## Open questions
- None blocking. `pnpm-workspace.yaml` still not needed (single package in `sdks/generator/`).
