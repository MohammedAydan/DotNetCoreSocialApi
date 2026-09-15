# Tasks — sdk-isolation

- [x] 1. Relocate generator engine to `sdks/generator/` (package.json, lock, orval.config, custom-instance, sdk-assets, scripts) with corrected paths + fresh `pnpm install`
- [x] 2. Relocate outputs (`web/`, `mobile/social_api_client`), rewrite root `.gitignore`, delete all root clutter (`node_modules`, SDK `src/`, `packages/`, root specs)
- [x] 3. Verify from zero (`generate:all`, typecheck, flutter analyze, root audit, dotnet build+test), close with review.md + living docs
- [ ] 2. Relocate outputs (`web/`, `mobile/social_api_client`), rewrite root `.gitignore`, delete all root clutter (`node_modules`, SDK `src/`, `packages/`, root specs)
- [ ] 3. Verify from zero (`generate:all`, typecheck, flutter analyze, root audit, dotnet build+test), close with review.md + living docs
