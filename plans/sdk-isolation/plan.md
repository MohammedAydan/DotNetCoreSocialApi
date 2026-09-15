# Plan — sdk-isolation

## Goal
Relocate the entire SDK generation pipeline from the repository root into a self-contained `sdks/` tree (`generator/` engine, `web/` TS output, `mobile/` Dart output) so the root contains zero Node/pnpm artifacts, with the full pipeline re-verified in its new home.

## Acceptance criteria (testable)
1. Root audit: no `package.json`, `pnpm-lock.yaml`, `node_modules/`, `orval.config.ts`, `tsconfig.json`, `openapi.json`, `openapitools.json`, `scripts/`, `sdk-assets/`, `packages/`, and no SDK-owned `src/` tree at root.
2. `cd sdks/generator && pnpm install && pnpm run generate:all` exits 0.
3. `pnpm run typecheck` (from `sdks/generator`) passes with 0 errors against `sdks/web`.
4. `flutter analyze` inside `sdks/mobile/social_api_client` reports 0 errors.
5. `dotnet build Social.sln -c Release` 0 errors and `dotnet test` 242/242 from root (backend untouched).

## Approach
1. Create `sdks/generator|web|mobile`; move engine files (`package.json`, `pnpm-lock.yaml`, `orval.config.ts`, `custom-instance.ts`, `sdk-assets/`, `scripts/`) into `sdks/generator/` and rewrite all relative paths for the new depth.
2. Adapt mission paths to repo reality: .NET project lives at `Social/` (not `src/Social.API/`), so the spec input is `../../Social/Social.API.json`; drop the intermediate `openapi.json` copy and point generators at the build artifact directly (mission Step 2/3 shape, corrected paths). Use `import.meta.dirname` (config is ESM — mission's `__dirname` would crash).
3. Keep the proven `generate-mobile.mjs` orchestrator (pubspec `^3.8.0` patch + factory restore from `sdk-assets/`), repathed to `../mobile/social_api_client`; keep package name `@social/sdk-generator`.
4. Move `packages/social_api_client` → `sdks/mobile/social_api_client` (drop stale `.dart_tool`, re-run pipeline); delete root `src/` SDK tree, `packages/`, root `node_modules/`, root spec files; fresh `pnpm install` inside `sdks/generator/`.
5. Rewrite root `.gitignore` SDK block for `sdks/...` locations (covering the real `dart-dio` layout `lib/src/...`); re-verify everything from zero; close with review + living docs.

## Scope
IN: file relocation, path/config rewrites, gitignore rewrite, root cleanup, full re-verification.
OUT: any .NET code changes; generator version bumps; behavior changes to generated output; CI wiring; Scalar/operationIds follow-ups (still deferred).

## Dependencies
- Toolchain as before: pnpm 10.33.2, Node 25, Flutter 3.47/Dart 3.13, Java 23, .NET 9 SDK.
- Requires a prior `dotnet build` (emits `Social/Social.API.json`, already present and gitignored); network for `pnpm install` only.

## Complexity
M (pure relocation + path surgery, but three runtimes must re-verify green).
