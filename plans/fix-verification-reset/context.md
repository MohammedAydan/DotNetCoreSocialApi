# Context: fix-verification-reset

## Files touched
- `Social.Infrastructure/Repositories/UserRepository.cs` — `UpdateUserAsync` lines 143-147
- `Social.Application/Features/Users/UserProfile.cs` — line 28 `CreateMap<UpdateUserDto, User>`
- `Social.Application/Features/Users/DTOs/UpdateUserDto.cs` — no IsVerified (correct, unchanged)
- `Social.Application/Features/Users/Commands/UpdateUserCommand.cs` — maps DTO->User then calls repo
- `Social.Core/Entities/User.cs` — `IsVerified = false` default (correct for new users)

## Key fact
`UpdateUserDto` intentionally omits `IsVerified`; only `AdminRepository.ToggleUserVerificationAsync` should mutate it.

## Open questions
- None. Fix is minimal and safe.

## Follow-ups (out of scope)
- `UpdateUserCommandHandler` forces `UserGender` to Male on every update (DTO has no gender field).
- `UpdateUserAsync` never updates `UserName` though DTO carries it.
