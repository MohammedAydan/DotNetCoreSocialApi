# Tasks: Diagnose & Fix Feed Endpoint UserId1 Runtime Error

- [x] Task 1: Search entire repository for `UserId1` across source, json, generated files, migrations, snapshots, bin, and obj
- [x] Task 2: Inspect all DbContexts in solution and verify which is injected into Feed service/handler
- [x] Task 3: Inspect the Feed query, handler, navigations, AutoMapper profiles, and capture generated SQL
- [x] Task 4: Programmatic model inspection of `Post` properties (`UserId` vs `UserId1`) and foreign keys
- [x] Task 5: Clean `bin`, `obj`, `publish`, then rebuild and publish `Social.API` & `Social.Infrastructure`
- [x] Task 6: Local runtime test of Feed endpoint and capture generated SQL
- [x] Task 7: Verify production deployment / IIS binaries (timestamps, hashes, publish comparison)
- [x] Task 8: Check IIS process status, ensure fresh process lifecycle without stale cache
- [x] Task 9: Verify 0 database schema changes (enforce `Posts.UserId`, no `UserId1`)
- [x] Task 10: End-to-end verification, review, and final comprehensive report
