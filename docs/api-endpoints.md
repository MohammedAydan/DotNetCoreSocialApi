# 🌐 API Endpoints Reference

The following is a breakdown of the primary API controllers and their respective responsibilities.

### 🔑 Authentication & Users
- `POST /api/User/register`: Create a new account.
- `POST /api/User/sign-in`: Login and receive JWT + Refresh Token.
- `GET /api/User/get-user`: Fetch current user profile.
- `GET /api/User/search?q={query}`: Search for users.
- `PUT /api/User/update-user`: Edit profile details.

### 📝 Posts Management
- `POST /api/Posts`: Create a post (supports media).
- `GET /api/Posts/feed`: Get a personalized feed of posts from followed users.
- `GET /api/Posts/user/{userId}`: View posts by a specific user.
- `POST /api/Posts/share`: Share/Repost an existing post.

### 💬 Social Interactions
- **Comments**:
    - `POST /api/Comments`: Comment on a post.
    - `POST /api/Comments/reply`: Reply to an existing comment.
    - `GET /api/Comments/post/{postId}`: List comments for a post.
- **Likes**:
    - `POST /api/Like`: Toggle like on a post.
    - `GET /api/Like/{postId}`: Get the list of users who liked a post.
- **Following**:
    - `POST /api/Follow/follow`: Send follow request.
    - `POST /api/Follow/accept-follow-request`: Accept a pending request.

### 🔔 Notifications
- `GET /api/Notifications/user/{userId}`: Retrieve all notifications for the user.
- `POST /api/Notifications/{id}/mark-read`: Mark a specific notification as viewed.

---
> [!TIP]
> Use the Swagger UI available at `/swagger` (in Development) to explore the full interactive documentation.
