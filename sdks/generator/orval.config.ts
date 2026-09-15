import { defineConfig } from 'orval';
import path from 'node:path';

// .NET 9 build-time OpenAPI artifact, emitted into Social/ by
// `OpenApiGenerateDocuments` on every `dotnet build` (gitignored).
// NOTE: ESM config — `__dirname` does not exist here; `import.meta.dirname`
// (Node 20.11+) is the correct equivalent.
const openApiTarget = path.resolve(
  import.meta.dirname,
  '../../Social/Social.API.json',
);

export default defineConfig({
  socialApi: {
    input: {
      target: openApiTarget,
    },
    output: {
      mode: 'tags-split',
      target: '../web/endpoints',
      schemas: '../web/models',
      client: 'react-query',
      httpClient: 'axios',
      mock: false,
      override: {
        mutator: {
          path: './custom-instance.ts',
          name: 'customInstance',
        },
        query: {
          useQuery: true,
          useMutation: true,
        },
      },
    },
  },
  socialValidation: {
    input: {
      target: openApiTarget,
    },
    output: {
      mode: 'tags-split',
      client: 'zod',
      target: '../web/validations',
    },
  },
});
