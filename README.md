
---

# 📘 Social API Documentation (v1)

### Base URL

```
https://localhost:7116/
```

### Overview

The Social API is a RESTful interface for a social media platform supporting:

* Posts, Comments, Likes
* Follow system
* Notifications
* User management

---

## 🔐 Authentication

> **Required for most endpoints.**
> Use Bearer Token in the `Authorization` header:

```
Authorization: Bearer <your-token>
```

---

## 📌 Endpoints

### 📄 Posts

| Action         | Method | Endpoint                   | Description                    |
| -------------- | ------ | -------------------------- | ------------------------------ |
| Create         | POST   | `/api/Posts`               | Create a new post              |
| Update         | PUT    | `/api/Posts`               | Update an existing post        |
| Share          | POST   | `/api/Posts/share`         | Share an existing post         |
| Get My Posts   | GET    | `/api/Posts/my-posts`      | Get own posts                  |
| Get User Posts | GET    | `/api/Posts/user/{userId}` | Get posts from a specific user |
| Get Feed       | GET    | `/api/Posts/feed`          | Posts from followed users      |
| Get by ID      | GET    | `/api/Posts/{postId}`      | Get a specific post            |
| Delete         | DELETE | `/api/Posts/{postId}`      | Delete a post                  |

---

### 💬 Comments

| Action        | Method | Endpoint                         | Description                 |
| ------------- | ------ | -------------------------------- | --------------------------- |
| Create        | POST   | `/api/Comments`                  | Comment on a post           |
| Reply         | POST   | `/api/Comments/reply`            | Reply to a comment          |
| Update        | PUT    | `/api/Comments/{commentId}`      | Update a comment            |
| Delete        | DELETE | `/api/Comments/{commentId}`      | Delete a comment            |
| Get by ID     | GET    | `/api/Comments/{commentId}`      | Get comment by ID           |
| Post Comments | GET    | `/api/Comments/post/{postId}`    | Comments on a specific post |
| Replies       | GET    | `/api/Comments/reply/{parentId}` | Replies to a comment        |

---

### 👥 Follow

| Action            | Method | Endpoint                              |
| ----------------- | ------ | ------------------------------------- |
| Follow a user     | POST   | `/api/Follow/follow`                  |
| Unfollow a user   | POST   | `/api/Follow/unfollow`                |
| Accept follow req | POST   | `/api/Follow/accept-follow-request`   |
| Reject follow req | POST   | `/api/Follow/reject-follow-request`   |
| Get followers     | GET    | `/api/Follow/followers`               |
| Get following     | GET    | `/api/Follow/following`               |
| Pending requests  | GET    | `/api/Follow/pending-follow-requests` |

---

### ❤️ Like

| Action      | Method | Endpoint             |
| ----------- | ------ | -------------------- |
| Like a post | POST   | `/api/Like`          |
| Get likes   | GET    | `/api/Like/{postId}` |

---

### 🔔 Notifications

| Action           | Method | Endpoint                                         |
| ---------------- | ------ | ------------------------------------------------ |
| Create           | POST   | `/api/Notifications`                             |
| Get by ID        | GET    | `/api/Notifications/{id}`                        |
| Update           | PUT    | `/api/Notifications/{id}`                        |
| Delete           | DELETE | `/api/Notifications/{id}`                        |
| Get for user     | GET    | `/api/Notifications/user/{userId}`               |
| Get unread       | GET    | `/api/Notifications/user/{userId}/unread`        |
| Get paged        | GET    | `/api/Notifications/user/{userId}/paged`         |
| Mark as read     | POST   | `/api/Notifications/{id}/mark-read`              |
| Mark all as read | POST   | `/api/Notifications/user/{userId}/mark-all-read` |
| Delete all       | DELETE | `/api/Notifications/user/{userId}/all`           |

---

### 👤 User

| Action         | Method | Endpoint                      |
| -------------- | ------ | ----------------------------- |
| Register       | POST   | `/api/User/register`          |
| Sign In        | POST   | `/api/User/sign-in`           |
| Current User   | GET    | `/api/User/get-user`          |
| User by ID     | GET    | `/api/User/get-user/{userId}` |
| Search Users   | GET    | `/api/User/search`            |
| Update Profile | PUT    | `/api/User/update-user`       |
| Delete User    | DELETE | `/api/User/delete-user`       |

---

## 📦 Schemas (Sample)

### `CreatePostRequest`

```json
{
  "title": "string",
  "content": "string",
  "visibility": "string",
  "media": [
    {
      "postId": "string",
      "name": "string",
      "type": "string",
      "url": "string",
      "thumbnailUrl": "string"
    }
  ]
}
```

### `UpdatePostRequest`

```json
{
  "id": "string",
  "title": "string",
  "content": "string",
  "visibility": "string",
  "media": [
    {
      "id": "string",
      "postId": "string",
      "userId": "string",
      "name": "string",
      "type": "string",
      "url": "string",
      "thumbnailUrl": "string",
      "createdAt": "2023-01-01T00:00:00Z",
      "updatedAt": "2023-01-01T00:00:00Z"
    }
  ]
}
```

---