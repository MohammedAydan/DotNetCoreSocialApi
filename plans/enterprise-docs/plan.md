# Plan — enterprise-docs

## Goal
Deep-scan the full repo and produce enterprise-grade technical documentation under `docs/` (architecture, exhaustive API reference, web SDK guide, mobile SDK guide, tooling/pipeline) plus a README master portal — zero hallucination, every endpoint cataloged.

## Acceptance criteria (testable)
1. `docs/ARCHITECTURE.md` documents soft-delete/masking, privacy/sharing, two-way block, atomic counters, MySQL/EF indexing, split-query rationale — each claim traceable to `ApplicationDbContext.cs` / `PostRepository.cs`.
2. `docs/API_REFERENCE.md` catalogs EVERY path+method in `Social/Social.API.json` with auth, params, bodies, responses, error matrix — count matches spec path count, no TODO stubs.
3. `docs/SDK_WEB.md` / `SDK_MOBILE.md` recipes reference real file names and real exported symbols in `sdks/web` and `sdks/mobile/social_api_client`.
4. `docs/TOOLING_AND_PIPELINE.md` documents isolation layout, build-before-generate order, CLI reference, CI example — commands verified against `sdks/generator/package.json` + `orval.config.ts`.
5. Root `README.md` rewritten as portal linking all five docs; all code blocks use language tags; markdown renders (no broken links).

## Approach
1. Parallel deep-scan via subagents: (a) backend invariants (DbContext + PostRepository + ADRs), (b) OpenAPI spec full endpoint inventory, (c) web SDK tree + generator configs, (d) mobile SDK tree + factory assets. Each returns evidence (file paths, symbol names, counts), not prose.
2. Main agent synthesizes the five docs directly from evidence, spot-checking 2–3 claims per doc against source.
3. Rewrite README portal; verify links/paths exist; close with review.md + SESSION_LOG.

## Scope
IN: five docs files, README rewrite, evidence-based verification.
OUT: any .NET/TS/Dart code changes; regenerating SDKs; applying migrations; CI workflow files (example snippet only).

## Dependencies
- `Social/Social.API.json` present (build artifact); `sdks/web`, `sdks/mobile` generated trees present; `plans/DECISIONS.md` ADRs 001–013.
- No new deps. No secrets needed.

## Complexity
L (large reading surface, but writing is mechanical once evidence is in).
