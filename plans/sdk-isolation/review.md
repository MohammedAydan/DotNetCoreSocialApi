# Review — sdk-isolation

## What was done
Relocated the entire SDK pipeline from repo root into `sdks/` with zero behavior change to generated output:
- `sdks/generator/` — engine: `package.json` (`@social/sdk-generator`), `pnpm-lock.yaml`, `orval.config.ts`, `tsconfig.json`, `custom-instance.ts`, `scripts/generate-mobile.mjs`, `sdk-assets/`, local `node_modules/`.
- `sdks/web/{endpoints,models,validations}/` — Orval output (flat per mission, no `generated/` middle dir).
- `sdks/mobile/social_api_client/` — dart-dio package + restored factory + 28 `.g.dart` files.
- Deleted from root: `node_modules/`, SDK `src/`, `scripts/`, `sdk-assets/`, `packages/`, `openapi.json`, `openapitools.json`, `package.json`, `pnpm-lock.yaml`, `orval.config.ts`, `tsconfig.json`. Root audit programmatically CLEAN.
- `.gitignore` SDK block rewritten for `sdks/...` locations and the real `lib/src/...` layout.

## Adaptations vs the mission brief (all load-bearing, verified)
1. **Paths**: repo has `Social/` + `Social.Tests/`, not `src/Social.API/` + `tests/`. Spec input is `../../Social/Social.API.json`; generators read the build artifact directly (no intermediate `openapi.json`; `export-openapi.mjs` deleted as dead code).
2. **ESM**: config has `"type": "module"` — mission's `__dirname` is undefined and would crash at load. Used `import.meta.dirname` (Node 20.11+); tsc confirms the typing resolves.
3. **Kept `generate-mobile.mjs`** instead of inlining the Java CLI: pubspec `^3.8.0` patch + factory restore are required for a working pipeline (proven last session; re-proven here).
4. **tsconfig `paths` map**: generated sources moved to `../web` but `node_modules` lives in the generator package, so bare `zod` / `@tanstack/react-query` imports failed (TS2307). Mapped the two observed bare specifiers to `./node_modules/*` (typecheck-only; consumers resolve via their own deps).
5. **Ignore block** covers real `dart-dio` layout (`lib/src/...`), not the brief's `lib/api/` guess.

## Incident: locked-directory move
`Move-Item packages/...` failed (transient OS file lock), and the chained `Remove-Item` then deleted the un-moved tree. No hand-written code was lost (factory source was already in `sdk-assets/`), and the from-zero regeneration that followed is a stronger proof than a move. Lesson recorded: never chain a delete after a move without verifying the move — pattern added to PATTERNS.md.

## Verification (all real)
- `pnpm install` + `pnpm run generate:all` from `sdks/generator`: exit 0 (Orval ×2, pubspec patch, factory restore, build_runner 84 outputs).
- `pnpm run typecheck`: 0 errors. `flutter analyze` (new path): 0 errors (same 11 upstream warnings).
- Root audit script: CLEAN. `dotnet build`: 0 errors. `dotnet test`: 242/242.

## Follow-ups (unchanged, still deferred)
operationIds, Scalar UI, CI wiring (`dotnet build` must precede `generate:all` — document in workflow).
