# SDK + Docs Refresh — Tasks

- [x] T1 Rebuild spec: `dotnet build Social.sln -c Release` 0 errors; confirm 6 report paths in `Social/Social.API.json`
- [x] T2 Regenerate: `pnpm run generate:all` from `sdks/generator` exit 0
- [x] T3 Verify gates: `pnpm run typecheck` 0 errors; `flutter analyze` 0 errors (inside `sdks/mobile/social_api_client`)
- [x] T4 Probe symbols: record real web hook names + zod schemas + Dart method names for the 6 report ops
- [x] T5 Docs: reporting recipes in `docs/SDK_WEB.md` + `docs/SDK_MOBILE.md` (real symbols only); count pointers in `TOOLING_AND_PIPELINE.md` if stale
- [x] T6 Parity + Close: API_REFERENCE 83/76 re-check, `dotnet test` 266/266, `review.md`, SESSION_LOG (incl. relocated post-reporting entry), context
