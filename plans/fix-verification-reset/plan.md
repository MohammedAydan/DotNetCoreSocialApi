# Plan: fix-verification-reset

## Goal
Preserve `User.IsVerified` when a user updates their own profile data via `PUT /api/User/update-user`.

## Root cause
- `UpdateUserDto` has no `IsVerified` field (correct).
- `CreateMap<UpdateUserDto, User>` leaves `User.IsVerified` at default `false` on the mapped entity.
- `UserRepository.UpdateUserAsync` (Social.Infrastructure/Repositories/UserRepository.cs:146-147) copies `user.IsVerified` onto `existingUser` whenever they differ, so a verified (`true`) account is reset to `false` on every profile update.

## Acceptance criteria
- [ ] Updating profile (FirstName/LastName/Bio/etc.) on a verified account keeps `IsVerified == true`.
- [ ] Regular profile update cannot set `IsVerified` (admin-only via `ToggleUserVerificationAsync`).
- [ ] `dotnet build Social.sln -c Release` 0 errors; relevant tests pass.

## Approach
1. Remove the `IsVerified` copy block from `UserRepository.UpdateUserAsync` (admin path owns verification).
2. Defense-in-depth: `Ignore()` `IsVerified` (plus identity/counter fields) on `UpdateUserDto -> User` map in `UserProfile.cs`.
3. Verify with build + targeted tests + full suite if fast.

## Scope
- IN: `UserRepository.cs`, `UserProfile.cs`, regression test if cheap.
- OUT: UserGender overwrite quirk, UserName update gap, DTO reshaping (follow-ups only).

## Complexity
S
