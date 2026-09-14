# 🗄️ Entities & Domain Documentation

## Core Entities

The domain layer contains the following primary entities representing the social network's data.

### 👤 User
- **Base**: Inherits from `IdentityUser`.
- **Properties**: `FirstName`, `LastName`, `Bio`, `ProfileImageUrl`, `FollowersCount`, `FollowingCount`.
- **Relationships**: 
    - Has many `Posts`.
    - Has many `Followers` / `Following`.

### 📝 Post
- **Properties**: `Title`, `Content`, `Visibility` (Public/Private), `LikesCount`, `CommentsCount`.
- **Relationships**:
    - Belongs to a `User`.
    - Can have multiple `Media` attachments.
    - Can be a "Shared" post via `ParentPostId`.

### 💬 Comment
- **Properties**: `Content`, `RepliesCount`, `IsDeleted`.
- **Relationships**:
    - Belongs to a `Post` and a `User`.
    - Can have a `ParentId` (Recursive relationship for replies).

### 👥 Follower
- **Role**: Represents the relationship between two users.
- **Properties**: `FollowerId`, `FollowingId`, `Accepted`.

### 🔔 Notification
- **Properties**: `Type` (Like, Follow, Comment), `Message`, `IsRead`.
- **Relationships**:
    - `UserId` (Sender).
    - `RecipientId` (Receiver).
    - Optional links to `PostId`, `CommentId`.

### 🖼️ Media
- **Properties**: `Name`, `Url`, `Type` (Image/Video).
- **Relationships**: Belongs to a `Post`.

## 🔄 ENUMs & Constants
- **VisibilityValues**: `Public`, `Private`.
- **UserGenderTypes**: `Male`, `Female`.
- **MediaTypes**: Defines supported formats.
