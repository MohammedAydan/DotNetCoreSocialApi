# Context — enterprise-docs

## Sources of truth (read, don't guess)
- Backend: `Social.Infrastructure/Data/ApplicationDbContext.cs`, `Social.Infrastructure/Repositories/PostRepository.cs`
- Spec: `Social/Social.API.json` (build artifact, gitignored)
- Web SDK: `sdks/web/endpoints/`, `sdks/web/models/`, `sdks/web/validations/`
- Mobile SDK: `sdks/mobile/social_api_client/lib/`, `sdks/generator/sdk-assets/`
- Tooling: `sdks/generator/package.json`, `sdks/generator/orval.config.ts`, `sdks/generator/custom-instance.ts`, `sdks/generator/scripts/generate-mobile.mjs`
- Decisions: `plans/DECISIONS.md` (ADR-001–013); existing docs in `docs/` are legacy/superseded — verify before reusing

## Outputs
- `docs/ARCHITECTURE.md`, `docs/API_REFERENCE.md`, `docs/SDK_WEB.md`, `docs/SDK_MOBILE.md`, `docs/TOOLING_AND_PIPELINE.md` (new/overwrite)
- Root `README.md` (rewrite as portal; existing file is a stale v1 endpoint list)
- Close: `plans/enterprise-docs/review.md`, `plans/context.md` touch, `plans/SESSION_LOG.md` append

## Open questions
- None blocking. If `Social.API.json` is stale vs controllers, note it and prefer the spec (it is the contract SDKs generate from).

## Learned (update during work)
- (pending scan results)
