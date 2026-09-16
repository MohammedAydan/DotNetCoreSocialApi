# SDK + Docs Refresh (post-reporting) — Plan

## Goal
Regenerate both SDKs from the post-reporting spec (83 ops / 76 paths) and update SDK/docs pages so reporting endpoints are usable from web + mobile with real symbols.

## Acceptance Criteria (testable)
1. `dotnet build Social.sln -c Release` 0 errors; fresh `Social/Social.API.json` contains the 6 report paths.
2. `pnpm run generate:all` from `sdks/generator` exits 0; `pnpm run typecheck` 0 errors; `flutter analyze` inside `sdks/mobile/social_api_client` 0 errors (≤11 known upstream warnings).
3. Generated web tree contains report hooks (real `use…` names recorded); generated Dart client contains report methods (real names recorded).
4. `docs/SDK_WEB.md` + `docs/SDK_MOBILE.md` each gain a reporting recipe with real hook/method/schema names + envelope-cast note; `docs/TOOLING_AND_PIPELINE.md` counts updated if stale; no invented symbols.
5. `API_REFERENCE.md` parity re-confirmed (83/76); full `dotnet test` stays 266/266 (no prod code changes expected).

## Approach
Build → generate → probe generated trees for real report symbols → patch SDK docs → verify gates → close. Solo (linear pipeline, no parallelizable units).

## Scope IN
Spec rebuild, `generate:all`, typecheck/analyze gates, SDK_WEB/SDK_MOBILE reporting recipes, count pointer updates, parity re-check.

## Scope OUT
New endpoints, prod migration/database update, prod deploy, operationIds, CI wiring, hand-editing generated trees.

## Dependencies
`sdks/generator` toolchain (pnpm install state from sdk-isolation); flutter for analyze (if absent, record as blocker and rely on generator exit 0 + tsc).

## Complexity
S (pipeline rerun + docs; no production code).
