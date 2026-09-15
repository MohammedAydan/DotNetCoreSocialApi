# Tooling & Pipeline — SDK Generation

> Sources: `sdks/generator/package.json`, `sdks/generator/orval.config.ts`, `sdks/generator/tsconfig.json`, `sdks/generator/custom-instance.ts`, `sdks/generator/scripts/generate-mobile.mjs`, `plans/DECISIONS.md` (ADR-012, ADR-013).

## 1. Monorepo directory isolation

```
repo-root/
├── Social.sln                  # .NET solution (untouched by the pipeline)
├── Social/                     # API host; build emits Social/Social.API.json (gitignored)
├── Social.Core|Application|Infrastructure|Tests/, Social.Admin.Web/
├── .gitignore                  # SDK block ignores sdks/web, mobile lib/src, node_modules
└── sdks/                       # THE ONLY place for SDK code
    ├── generator/              # pipeline engine (the pnpm package)
    │   ├── package.json        # @social/sdk-generator (private)
    │   ├── pnpm-lock.yaml
    │   ├── orval.config.ts     # dual Orval config (react-query + zod)
    │   ├── custom-instance.ts  # Axios mutator (AbortController-native)
    │   ├── tsconfig.json       # strict + paths map for ../web typecheck
    │   ├── sdk-assets/flutter/api_client_factory.dart   # factory source of truth
    │   └── scripts/generate-mobile.mjs                  # mobile orchestrator
    ├── web/                    # generated TS (endpoints/ models/ validations/)
    └── mobile/social_api_client/  # generated Dart package (+ restored factory)
```

Strict rule: **ZERO Node/pnpm files in root** — no `package.json`, `pnpm-lock.yaml`, `node_modules/`, `orval.config.ts`, `tsconfig.json`, or loose spec JSON at the repository root (audit-verified in sdk-isolation; ADR-013). `pnpm` is the only package manager (never npm/yarn). No `pnpm-workspace.yaml` is needed — `sdks/generator` is a single package.

Generated trees are **gitignored, generate-at-build-time** (ADR-012): `sdks/web/endpoints|models|validations/`, mobile `lib/src/`, `lib/social_api_client.dart`, `doc/`, `test/`, `.dart_tool/`, `.openapi-generator/`, `build/`, `pubspec.lock`. Committed (hand-written): everything under `sdks/generator` except `node_modules/` and `openapitools.json` (the CLI version-manifest cache, recreated CWD-relative on each run), plus the restored `lib/api_client_factory.dart` copy's source in `sdk-assets/`.

## 2. Build-time generation order (strict)

The generators do **not** call the live API. They read the .NET 9 build artifact:

```
dotnet build Social.sln -c Release
   └─► Social/Social.API.json          (OpenApiGenerateDocuments + ApiDescription.Server 9.0.4)
        ┌─► orval  ──► sdks/web/...
        └─► openapi-generator-cli (dart-dio) ──► sdks/mobile/social_api_client/...
```

`dotnet build` **MUST** run before `pnpm run generate:all` — otherwise the input is stale or missing. `MapOpenApi()` is unconditional (Scalar/SwaggerUI stays dev-only). A document transformer strips `OpenApiNull` `default: null` entries at the source (7 occurrences): Orval's zod emitter turned them into un-typable `.default(null)` (ADR-012). Fixing at the .NET source benefits every consumer, including live `/openapi/v1.json` — not just the export.

## 3. Toolchain (pinned, verified)

| Tool | Version | Note |
|------|---------|------|
| .NET SDK | 9.0.310 | `Microsoft.Extensions.ApiDescription.Server` 9.0.4 |
| Node | v25.9.0 | ESM config → `import.meta.dirname` (`__dirname` would crash) |
| pnpm | 10.33.2 | `packageManager: pnpm@10.33.2` |
| orval | 8.33.0 (`^8.21.0`, CVE-2026-72717 fixed) | 8.33 dropped `input.validation` — key removed, validation default-on |
| openapi-generator-cli (npm wrapper) | `^2.41.0` | npm `^7.22.0` does **not** exist; wrapper 2.41.0 bundles Java generator **7.25.0** (verified via CLI `version`) |
| TypeScript | `^5.7.2` | strict, `noUncheckedIndexedAccess` |
| Axios / zod / TanStack Query | `^1.7.9` / `^3.23.8` / `^5.62.0` | react-query v5 hooks; Bearer via interceptor |
| Flutter / Dart / Java | 3.47.0 / 3.13.0 / 23 | pubspec patched to `^3.8.0` (see §5) |

## 4. CLI reference (run from `sdks/generator/`)

```bash
cd sdks/generator
pnpm install            # fresh install into sdks/generator/node_modules
pnpm run generate:web   # orval → ../web/{endpoints,models,validations} (tags-split)
pnpm run generate:mobile# node scripts/generate-mobile.mjs (see §5)
pnpm run generate:all   # web && mobile (exit 0 = both reproduced from zero)
pnpm run typecheck       # tsc --noEmit over orval.config + custom-instance + ../web
```

`orval.config.ts` (dual config, same `input.target = ../../Social/Social.API.json` resolved via `import.meta.dirname`):

- `socialApi`: `mode: tags-split`, `target: ../web/endpoints`, `schemas: ../web/models`, `client: react-query`, `httpClient: axios`, `mock: false`, `override.mutator: { path: ./custom-instance.ts, name: customInstance }`, `query: { useQuery: true, useMutation: true }`.
- `socialValidation`: `mode: tags-split`, `client: zod`, `target: ../web/validations`.

`tsconfig.json` includes `["orval.config.ts", "custom-instance.ts", "../web"]` with a typecheck-only `paths` map for the two bare imports Orval emits (`zod`, `@tanstack/react-query` → `./node_modules/...`): generated sources live in `../web` but dependencies are installed in the generator package (ADR-013; consumers resolve via their own deps, no workspace/junction hacks).

Verification gates: `pnpm run typecheck` → 0 errors; `flutter analyze` **inside** `sdks/mobile/social_api_client` → 0 errors (11 upstream `unused_import` warnings accepted as generator-template noise); `dotnet build` → 0 errors; `dotnet test` → 242/242.

## 5. What `generate:mobile` actually does

`scripts/generate-mobile.mjs` (kept as a script instead of an inlined CLI one-liner because three steps are load-bearing):

1. **Generate** (`cwd = sdks/generator/`):
   ```
   pnpm exec openapi-generator-cli generate -i ../../Social/Social.API.json -g dart-dio
     -o ../mobile/social_api_client --skip-validate-spec
     --additional-properties=pubName=social_api_client,pubLibrary=social_api_client,
       pubAuthor=SocialArchitectureTeam,pubVersion=1.0.0,
       serializationLibrary=json_serializable,dateLibrary=core,nullSafe=true,finalProperties=true
   ```
2. **Patch pubspec** (`sdk: '>=3.5.0 <4.0.0'` → `sdk: '^3.8.0'`, regex `/sdk:\s*'>=3\.5\.0 <4\.0\.0'/`): the generator emits language version 3.5, but `json_serializable ^6.9.3` output uses null-aware elements (`?instance.field`) requiring 3.8+. Without the patch `build_runner` cannot format.
3. **Restore hand-written assets** wiped by regeneration: `copyFileSync(../sdk-assets/flutter/api_client_factory.dart → lib/api_client_factory.dart)` and re-register `lib/api_client_factory.dart` in `.openapi-generator-ignore` (source of truth is `sdk-assets/`, committed; the generated tree is disposable).
4. **Install + codegen:** `flutter pub get`, then `dart run build_runner build --delete-conflicting-outputs` (84 outputs, 28 `.g.dart` files).

The Axios mutator (`custom-instance.ts`) is AbortController-native: `axios.create({ baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000' })`, request interceptor injects `Authorization: Bearer <token>` from `localStorage`/`sessionStorage` `access_token` (browser-guarded), and react-query forwards its `signal` inside `config` — no deprecated `CancelToken` anywhere.

## 6. CI/CD integration

Regenerate-and-diff keeps SDKs honest with backend changes. The ordering constraint (§2) means the workflow builds .NET first, then generates, then fails on drift:

```yaml
# .github/workflows/sdk-contract.yml (example — wire-up is a deferred follow-up)
name: sdk-contract
on:
  pull_request:
    paths: ['Social/**', 'Social.Core/**', 'Social.Application/**',
            'Social.Infrastructure/**', 'sdks/**']
jobs:
  contract:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '9.0.x' }
      - uses: pnpm/action-setup@v4
        with: { version: 10.33.2 }
      - uses: actions/setup-node@v4
        with: { node-version: 25, cache: pnpm, cache-dependency-path: sdks/generator/pnpm-lock.yaml }
      - uses: subosito/flutter-action@v2   # only needed for the mobile gate
        with: { flutter-version: '3.47.0', channel: stable }
      - run: dotnet build Social.sln -c Release
      - run: pnpm install
        working-directory: sdks/generator
      - run: pnpm run generate:all
        working-directory: sdks/generator
      - run: pnpm run typecheck
        working-directory: sdks/generator
      - run: flutter analyze --no-fatal-warnings
        working-directory: sdks/mobile/social_api_client
      - run: git diff --exit-code -- sdks/web sdks/mobile
        # exit 1 = spec changed without regenerated SDKs (or stale commit) — instruct
        # the author to run pnpm run generate:all locally and commit the result.
        # NOTE: generated trees are currently gitignored; to enforce this gate,
        # either lift the sdks/web|mobile ignores or cache the trees as artifacts.
```

Deferred follow-ups (unchanged): stable `operationId`s (currently empty — Orval auto-names are stable but verbose), Scalar UI, wiring this workflow in.
