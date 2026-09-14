# Cache Invalidation Strategy

This document outlines the cache invalidation patterns and conventions used throughout the Social API application.

## Cache Key Naming Convention

All cache keys follow a consistent pattern to enable efficient invalidation and pattern-based removal:

```
Entity Type    | Cache Key Pattern                  | Example
---------------|------------------------------------|---------------------------------
Single Entity  | {entity}:{id}                      | user:123, post:456
Entity Lists   | {entity}:list:{filter}             | post:list:page1, users:list:all
User-Related   | user:{userId}:{entity}             | user:123:posts, user:123:followers
Relationships  | {entity1}:{id1}:{entity2}:{id2}    | user:123:following:456
Tokens         | blacklisted_token:{token_hash}     | blacklisted_token:abc123...
```

## Cache Expiration Times

Configured based on data volatility and importance:

| Data Type            | TTL      | Reason                                    |
|----------------------|----------|-------------------------------------------|
| User Profiles        | 15 min   | High priority, moderate change frequency  |
| Posts                | 5 min    | Frequent updates (likes, comments)        |
| Comments             | 3 min    | Dynamic content                           |
| Notifications        | 2 min    | Time-sensitive                            |
| Blacklisted Tokens   | JWT exp  | Must match token expiration              |
| Statistics/Aggregates| 10 min   | Computationally expensive queries         |

## Invalidation Patterns

### Pattern 1: Direct Key Invalidation
When a single entity is updated, remove its specific cache entry.

```csharp
// Example: Update user profile
await _userRepository.UpdateUserAsync(user);
await _cacheService.RemoveAsync($"user:{user.Id}");
```

### Pattern 2: Pattern-Based Invalidation
When an operation affects multiple related cache entries, use pattern matching.

```csharp
// Example: User posts a new item - invalidate all post lists
await _postRepository.CreatePostAsync(post);
await _cacheService.RemoveByPatternAsync("post:list:*");
await _cacheService.RemoveByPatternAsync($"user:{userId}:posts*");
```

### Pattern 3: Cascade Invalidation
When an entity update affects related entities, invalidate all impacted caches.

```csharp
// Example: Delete a post - invalidate post, comments, likes, and user's post list
await _postRepository.DeletePostAsync(postId);
await _cacheService.RemoveAsync($"post:{postId}");
await _cacheService.RemoveByPatternAsync($"post:{postId}:comments*");
await _cacheService.RemoveByPatternAsync($"post:{postId}:likes*");
await _cacheService.RemoveByPatternAsync($"user:{userId}:posts*");
```

## Implementation Guidelines

### Commands (Write Operations)

ALL Command handlers that modify data MUST invalidate affected caches:

#### CreateUserCommand
```csharp
public async Task<ApiResponse<UserProfileDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
{
    var user = await _userRepository.CreateUserAsync(request.User);

    // Cache will be populated on first read (cache-aside pattern)
    // No explicit cache set required

    return ApiResponse<UserProfileDto>.SuccessResponse("User created", userDto);
}
```

#### UpdateUserCommand
```csharp
public async Task<ApiResponse<UserProfileDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
{
    await _userRepository.UpdateUserAsync(user);

    // Invalidate user cache
    await _cacheService.RemoveAsync($"user:{userId}");

    // Invalidate user lists that might include this user
    await _cacheService.RemoveByPatternAsync("users:list:*");

    return ApiResponse<UserProfileDto>.SuccessResponse("User updated", userDto);
}
```

#### CreatePostCommand
```csharp
public async Task<ApiResponse<PostDto>> Handle(CreatePostCommand request, CancellationToken cancellationToken)
{
    var post = await _postRepository.CreatePostAsync(request.Post);

    // Invalidate all post lists
    await _cacheService.RemoveByPatternAsync("post:list:*");

    // Invalidate user's post list
    await _cacheService.RemoveByPatternAsync($"user:{userId}:posts*");

    return ApiResponse<PostDto>.SuccessResponse("Post created", postDto);
}
```

#### DeletePostCommand
```csharp
public async Task<ApiResponse<bool>> Handle(DeletePostCommand request, CancellationToken cancellationToken)
{
    await _postRepository.DeletePostAsync(postId);

    // Cascade invalidation
    await _cacheService.RemoveAsync($"post:{postId}");
    await _cacheService.RemoveByPatternAsync($"post:{postId}:*");
    await _cacheService.RemoveByPatternAsync($"user:{userId}:posts*");

    return ApiResponse<bool>.SuccessResponse("Post deleted", true);
}
```

#### LikePostCommand / UnlikePostCommand
```csharp
public async Task<ApiResponse<bool>> Handle(LikePostCommand request, CancellationToken cancellationToken)
{
    await _likeRepository.LikePostAsync(userId, postId);

    // Invalidate post cache (like count changed)
    await _cacheService.RemoveAsync($"post:{postId}");

    // Invalidate user's liked posts list
    await _cacheService.RemoveByPatternAsync($"user:{userId}:likes*");

    return ApiResponse<bool>.SuccessResponse("Post liked", true);
}
```

#### CreateCommentCommand
```csharp
public async Task<ApiResponse<CommentDto>> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
{
    var comment = await _commentRepository.CreateCommentAsync(request.Comment);

    // Invalidate post cache (comment count changed)
    await _cacheService.RemoveAsync($"post:{postId}");

    // Invalidate post's comment list
    await _cacheService.RemoveByPatternAsync($"post:{postId}:comments*");

    return ApiResponse<CommentDto>.SuccessResponse("Comment created", commentDto);
}
```

#### FollowUserCommand / UnfollowUserCommand
```csharp
public async Task<ApiResponse<bool>> Handle(FollowUserCommand request, CancellationToken cancellationToken)
{
    await _followRepository.FollowUserAsync(followerId, followingId);

    // Invalidate follower's following list
    await _cacheService.RemoveByPatternAsync($"user:{followerId}:following*");

    // Invalidate following user's followers list
    await _cacheService.RemoveByPatternAsync($"user:{followingId}:followers*");

    // Invalidate follower count caches
    await _cacheService.RemoveAsync($"user:{followerId}");
    await _cacheService.RemoveAsync($"user:{followingId}");

    return ApiResponse<bool>.SuccessResponse("User followed", true);
}
```

### Queries (Read Operations)

Queries should implement cache-aside pattern:

```csharp
public async Task<UserProfileDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
{
    var cacheKey = $"user:{request.UserId}";

    // Try to get from cache
    var cachedUser = await _cacheService.GetAsync<UserProfileDto>(cacheKey);
    if (cachedUser != null)
        return cachedUser;

    // Cache miss - get from database
    var user = await _userRepository.GetUserByIdAsync(request.UserId);
    var userDto = _mapper.Map<UserProfileDto>(user);

    // Store in cache with appropriate TTL
    await _cacheService.SetAsync(cacheKey, userDto, TimeSpan.FromMinutes(15));

    return userDto;
}
```

## Testing Cache Invalidation

### Manual Testing Checklist

For each write operation, verify:

1. ✅ Entity is updated in database
2. ✅ Direct cache key is removed
3. ✅ Pattern-based caches are removed
4. ✅ Next read fetches fresh data
5. ✅ Fresh data is cached correctly

### Example Test Scenario

```
1. Get user profile (cache miss, loads from DB, caches)
2. Get user profile again (cache hit)
3. Update user profile
4. Verify cache was invalidated
5. Get user profile (cache miss, loads updated data, caches)
```

## Cache Service Implementation

Both RedisCacheService and InMemoryCacheService support:

- `GetAsync<T>(string key)` - Retrieve cached value
- `SetAsync<T>(string key, T value, TimeSpan? expiration)` - Store value with TTL
- `RemoveAsync(string key)` - Remove single key
- `RemoveByPatternAsync(string pattern)` - Remove keys matching pattern (e.g., "user:*")
- `ExistsAsync(string key)` - Check if key exists

## Monitoring and Debugging

### Logging

Cache operations are logged at DEBUG level:
- Cache hits/misses
- Cache set operations
- Cache removal operations

### Common Issues

**Issue**: Stale data served after update
**Solution**: Verify invalidation is called after database update, check cache key matches

**Issue**: Cache grows too large (in-memory)
**Solution**: Verify expiration times are set, implement eviction policy

**Issue**: Concurrent updates cause race conditions
**Solution**: Use optimistic concurrency in database, invalidate cache after successful DB update

## Best Practices

1. **Always invalidate after successful database write**, never before
2. **Use consistent key naming** following the convention above
3. **Set appropriate TTLs** based on data volatility
4. **Use pattern-based removal** for related data
5. **Log cache operations** for debugging
6. **Handle cache failures gracefully** - cache should enhance performance, not break functionality
7. **Document cache keys** for each feature with comments
8. **Test invalidation** as part of integration tests

## Migration Path

When adding caching to existing features:

1. Identify all read operations (Queries)
2. Add cache-aside pattern to Queries
3. Identify all write operations (Commands)
4. Add invalidation logic to Commands
5. Test the complete flow (read → write → read)
6. Monitor cache hit rates and adjust TTLs

---

*Last Updated: February 9, 2026*
*For questions or updates, refer to the development team*
