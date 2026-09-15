# Feature Plan: Rich Post Display, Granular Controls & Moderation Notifications

## Goal
Elevate post management and content moderation across the platform with rich media display (images, videos), granular post controls (visibility toggle, permanent delete, visibility status), automated author notifications with moderation rationale, filterable & searchable post feeds, and post inspection drawer/modal in the admin dashboard.

## Acceptance Criteria
1. **Rich Post Media & Content Display:**
   - Moderation feed and dashboard post cards display full post content, title, author info (username, email, ID), visibility badge, engagement counts (likes, comments, shares), and full media gallery.
   - Multiple images display cleanly in a responsive thumbnail gallery with click-to-preview lightbox.
   - Video media assets display with HTML5 native video player controls.
2. **Granular Post Controls:**
   - Instant visibility toggle (Hide / Restore) with reason input and confirmation.
   - Ability to update post visibility mode (`public`, `private`, `followers_only`).
   - Ability to permanently delete a post if required (with confirmation dialog).
3. **Automated User Moderation Notifications:**
   - When an administrator hides or restores a post or comment, an in-app `Notification` is automatically created for the content author with type `"ModerationNotice"`, explaining the action and reason.
4. **Enhanced Dashboard Moderation Experience:**
   - Filter feed by Status (All, Visible, Hidden), Type (All, Post, Comment), or Media (All, With Media, Text Only).
   - Search moderation feed by author or keyword.
   - Post Inspection Modal: click any post to view high-resolution media, author details, metadata, and quick moderation actions.
5. **Quality & Test Integrity:**
   - Clean Architecture preserved with zero layer violations.
   - All existing 138 automated tests continue to pass with 0 failures (`dotnet test Social.sln -c Release`).
   - New unit & integration tests added covering moderation notifications and extended post controls.
   - Clean compilation in Release mode (0 compiler errors).

## Scope
- **IN SCOPE:**
  - Update `ModerationFeedItemRecord` in `Social.Core/Interfaces/IAdminRepository.cs` to include media items (`ModerationMediaRecord`), title, visibility, and author details.
  - Update `AdminRepository.GetModerationFeedAsync` to eagerly load `p.Media` and project full media assets and post details.
  - Update `AdminModerationItemDto` in `Social.Application` to include `Title`, `Visibility`, and `Media` (`AdminMediaDto`).
  - Update `HidePostCommandHandler`, `RestorePostCommandHandler`, `HideCommentCommandHandler`, and `RestoreCommentCommandHandler` to inject `INotificationRepository` and dispatch in-app notifications to content authors with moderation reasons.
  - Add `UpdatePostVisibilityCommand` and `DeletePostPermanentlyCommand` with corresponding endpoints in `AdminModerationController.cs`.
  - Update `AdminDashboardController.cs` and dashboard UI with the rich media layout, video player, image gallery, post inspection modal, filter bar, and notification feedback.
  - Add test suites verifying the notification creation and new post controls.
- **OUT OF SCOPE:**
  - Real-time WebSockets/SignalR push (existing polling and REST notifications are used).
  - External email dispatch for moderation notices (in-app notifications are used).

## Complexity
Medium (M)
