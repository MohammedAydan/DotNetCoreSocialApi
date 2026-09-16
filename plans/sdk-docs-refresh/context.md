# SDK + Docs Refresh — Context

## Files to touch
- Generated trees (`sdks/web/**`, `sdks/mobile/social_api_client/lib/src/**`) — regenerated only, never hand-edited.
- `docs/SDK_WEB.md`, `docs/SDK_MOBILE.md` — append reporting recipes.
- `docs/TOOLING_AND_PIPELINE.md` — count pointers only if they cite 77/70/242.
- `docs/API_REFERENCE.md` — read-only parity check (already at 83/76).
- `plans/sdk-docs-refresh/*`, `plans/SESSION_LOG.md`, `plans/context.md`.

## Commands (order matters)
1. `dotnet build Social.sln -c Release` (emits spec input — NEVER generate first)
2. `cd sdks/generator && pnpm run generate:all`
3. `pnpm run typecheck` (in `sdks/generator`); `flutter analyze` (inside `sdks/mobile/social_api_client`)
4. `dotnet test Social.sln -c Release` (expect 266/266, unchanged)

## Conventions (from compass — do not invent)
- Spec has zero operationIds → web hooks are `use`+Method+Path; body-writes are useQuery-style, param-GETs are useMutation with `{params}`; all responses type `void` → cast to envelope.
- Dart factory source is `sdk-assets/`, never `lib/`.

## Open questions
- None. Flutter presence unverified — check at T3.
