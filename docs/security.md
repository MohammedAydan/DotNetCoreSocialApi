# 🛡️ Security & Authentication

## Authentication
- **Framework**: `Microsoft.AspNetCore.Identity`.
- **Method**: JWT (JSON Web Tokens).
- **Flow**:
  1. User logs in with Email/Password.
  2. Server returns `AccessToken` (short-lived) and `RefreshToken` (long-lived).
  3. Client includes token in `Authorization: Bearer <token>` header.

## 🔐 Token Security
- **Blacklisting**: `TokenBlacklistMiddleware` prevents the use of logged-out or rotated tokens.
- **Refresh Flow**: Refresh tokens are stored in the database and linked to the user's ID.

## 🏢 Middleware Chain
1. **CORS**: Enforces origin restrictions.
2. **Rate Limiting**: Throttles potentially malicious traffic.
3. **Blacklist Check**: Ensures the token hasn't been revoked.
4. **Authentication**: Validates the JWT signature and claims.
5. **Authorization**: Checks roles and policies.
