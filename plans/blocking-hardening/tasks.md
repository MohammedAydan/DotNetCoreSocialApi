# Tasks: blocking-hardening

- [x] 1. Follow block gates (FollowUserAsync, AcceptFollowRequestAsync, list filtering)
- [x] 2. Like block gates (AddLike path + notification suppress)
- [x] 3. Comment block gates (AddComment/AddReply + notification suppress)
- [x] 4. User profile/search block gates (GetUserById viewer check, SearchUsers exclusion) + notification command gate
- [x] 5. Admin ban hardening (reason/duration validation, refuse Admin targets, TTL align, audit duration)
- [x] 6. Admin unban fix (drop hardcoded ID, correct locked check, full unlock)
- [x] 7. Token kill (middleware blacklisted_user check + refresh lockout check)
- [x] 8. Build + full test suite + regression tests
