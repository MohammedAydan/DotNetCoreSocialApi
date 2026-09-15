// Orchestrates Dart `dart-dio` client generation + SDK-constraint patch +
// factory restore + dependency install + serialization codegen.
// Run via `pnpm run generate:mobile` from sdks/generator/.
//
// Why the pubspec patch? The generator emits `sdk: '>=3.5.0 <4.0.0'`
// (language version 3.5), but the generated `json_serializable ^6.9.3`
// output uses null-aware-elements (`?instance.field`), which requires a
// package language version of 3.8+. Bumping the lower bound to ^3.8.0
// aligns the language version without touching any generated source.
import { execSync } from 'node:child_process';
import { copyFileSync, readFileSync, writeFileSync } from 'node:fs';

// sdks/generator/scripts/ -> sdks/mobile/social_api_client/
const PKG_DIR = new URL('../../mobile/social_api_client/', import.meta.url);
const PUBSPEC = new URL('pubspec.yaml', PKG_DIR);

function run(cmd, cwd) {
  console.log(`[generate:mobile] $ ${cmd}`);
  execSync(cmd, { stdio: 'inherit', cwd, shell: true });
}

run(
  'pnpm exec openapi-generator-cli generate' +
    // .NET 9 build-time artifact: sdks/generator/ -> Social/Social.API.json
    ' -i ../../Social/Social.API.json' +
    ' -g dart-dio' +
    ' -o ../mobile/social_api_client' +
    ' --skip-validate-spec' +
    ' --additional-properties=pubName=social_api_client,pubLibrary=social_api_client,pubAuthor=SocialArchitectureTeam,pubVersion=1.0.0,serializationLibrary=json_serializable,dateLibrary=core,nullSafe=true,finalProperties=true',
  // cwd: sdks/generator/
  new URL('../', import.meta.url),
);

const before = readFileSync(PUBSPEC, 'utf8');
const after = before.replace(
  /sdk:\s*'>=3\.5\.0 <4\.0\.0'/,
  "sdk: '^3.8.0'",
);
if (after === before) {
  console.warn('[generate:mobile] pubspec SDK constraint pattern not found; leaving pubspec.yaml untouched');
} else {
  writeFileSync(PUBSPEC, after);
  console.log("[generate:mobile] patched pubspec SDK constraint to '^3.8.0'");
}

// Restore hand-written assets wiped by regeneration. Source of truth lives
// in sdks/generator/sdk-assets/ (committed); the generated tree is disposable.
copyFileSync(
  new URL('../sdk-assets/flutter/api_client_factory.dart', import.meta.url),
  new URL('lib/api_client_factory.dart', PKG_DIR),
);
console.log('[generate:mobile] restored lib/api_client_factory.dart');

const IGNORE = new URL('.openapi-generator-ignore', PKG_DIR);
const ignoreEntry = 'lib/api_client_factory.dart';
let ignore = readFileSync(IGNORE, 'utf8');
if (!ignore.includes(ignoreEntry)) {
  if (!ignore.endsWith('\n')) ignore += '\n';
  ignore += `# Hand-written factory: must survive regeneration.\n${ignoreEntry}\n`;
  writeFileSync(IGNORE, ignore);
  console.log('[generate:mobile] registered factory in .openapi-generator-ignore');
}

run('flutter pub get', PKG_DIR);
run('dart run build_runner build --delete-conflicting-outputs', PKG_DIR);
console.log('[generate:mobile] done');
