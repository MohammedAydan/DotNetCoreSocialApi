# Context: Rich Post Display, Granular Controls & Moderation Notifications

## Requirements & Scope
- **User Request:**
  - Improve the way posts are displayed: practical and organized layout, full content including images or videos.
  - Ability to toggle visibility (showing or hiding it) as needed.
  - Extensive capabilities via dashboard for comprehensive post control rather than just basic options.
  - Notify the user if a post has been blocked or removed, providing a reason via notification.
  - No major structural breaking changes elsewhere.

## Files to Modify / Create
- `Social.Core/Interfaces/IAdminRepository.cs` (add `ModerationMediaRecord`, update `ModerationFeedItemRecord`, add visibility & delete methods)
- `Social.Infrastructure/Repositories/AdminRepository.cs` (eagerly load `Media`, populate `ModerationMediaRecord`, implement visibility update and permanent delete)
- `Social.Application/Features/Admin/Moderation/DTOs/AdminModerationItemDto.cs` (add `AdminMediaDto`, `Title`, `Visibility`, `SharesCount`)
- `Social.Application/Features/Admin/Moderation/Queries/GetModerationFeedQuery.cs` (map media records and new properties to DTO)
- `Social.Application/Features/Admin/Moderation/Commands/HidePostCommand.cs` (dispatch in-app `Notification` to author)
- `Social.Application/Features/Admin/Moderation/Commands/RestorePostCommand.cs` (dispatch in-app `Notification` to author)
- `Social.Application/Features/Admin/Moderation/Commands/HideCommentCommand.cs` (dispatch in-app `Notification` to author)
- `Social.Application/Features/Admin/Moderation/Commands/RestoreCommentCommand.cs` (dispatch in-app `Notification` to author)
- `Social.Application/Features/Admin/Moderation/Commands/UpdatePostVisibilityCommand.cs` [NEW]
- `Social.Application/Features/Admin/Moderation/Commands/DeletePostPermanentlyCommand.cs` [NEW]
- `Social/Controllers/Admin/AdminModerationController.cs` (expose visibility and delete endpoints)
- `Social/Controllers/Admin/AdminDashboardController.cs` (render rich post cards with responsive image grid, HTML5 video player, moderation modal, filters)
- `Social.Tests/Infrastructure/TestAdminDoubles.cs` (update double implementations)
- `Social.Tests/Unit/Admin/ModerationNotificationTests.cs` [NEW]

## Dependencies Added
- None.
