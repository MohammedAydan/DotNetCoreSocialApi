# استراتيجية الكاش - 30MB Memory Optimized

## 🎯 الهدف
تقليل استخدام الذاكرة إلى أقل من 30MB مع الحفاظ على Performance للعمليات الأكثر أهمية.

## ✅ ما يتم تخزينه في الكاش (High Priority)

### 1. **User Profiles** - 15 دقيقة
- `user:profile:{userId}` - معلومات المستخدم الفردية
- **السبب**: بيانات صغيرة (< 2KB) ومطلوبة بكثرة
- **Endpoints**: GetUser, GetUserById

### 2. **Single Post** - 5 دقائق
- `post:{postId}:user:{userId}` - منشور واحد فقط
- **السبب**: حجم متوسط (~5-10KB) لكنه مهم للـ engagement
- **Endpoints**: GetPostById

### 3. **Single Comment** - 3 دقائق
- `comment:{commentId}` - تعليق واحد فقط
- **السبب**: حجم صغير (~1-2KB)
- **Endpoints**: GetCommentById

### 4. **Single Notification** - 2 دقيقة
- `notification:{id}` - إشعار واحد
- **السبب**: حجم صغير جداً (~500B-1KB)
- **Endpoints**: GetById

## ❌ ما لا يتم تخزينه (Removed - Low Priority)

### القوائم الكبيرة (Large Lists)
- ❌ Posts Feed/Lists
- ❌ Comments Lists
- ❌ Followers/Following Lists
- ❌ Notifications Lists
- ❌ Likes Lists
- ❌ Pending Follow Requests
- ❌ Search Results

**السبب**: تستهلك ذاكرة كبيرة (20-100KB+ لكل صفحة) × عدد المستخدمين

### البيانات المتغيرة باستمرار
- ❌ Paginated Results (كل صفحة = كاش منفصل)
- ❌ Dynamic Feeds

## 📊 تقدير استخدام الذاكرة

### الحد الأقصى المتوقع (30MB Redis)
```
User Profiles:     1,000 users × 2KB   = 2 MB
Single Posts:      5,000 posts × 8KB   = 40 MB  ❌ Too much!
Single Comments:   3,000 × 1.5KB       = 4.5 MB
Notifications:     2,000 × 1KB         = 2 MB
```

### الاستراتيجية المعدلة (تحت 30MB)
```
User Profiles:     1,500 users × 2KB   = 3 MB    ✅
Single Posts:      2,000 posts × 8KB   = 16 MB   ✅
Single Comments:   2,000 × 1.5KB       = 3 MB    ✅
Notifications:     3,000 × 1KB         = 3 MB    ✅
---------------------------------------------------
Total:                                   25 MB    ✅
```

## 🔄 Cache Invalidation

### بعد التعديل (Simplified)
- **UpdateUser**: يحذف `user:profile:{userId}` + `post:*` (pattern)
- **UpdatePost**: يحذف `post:{postId}:user:{userId}` فقط
- **UpdateComment**: يحذف `comment:{commentId}` فقط
- **UpdateNotification**: يحذف `notification:{id}` فقط

### بعد الحذف
- **DeleteUser**: يحذف profile + patterns
- **DeletePost**: يحذف single post
- **DeleteComment**: يحذف single comment

## 🚀 Performance Trade-offs

### ✅ المكاسب
- استهلاك ذاكرة أقل بكثير (~25MB بدلاً من 100MB+)
- TTL أطول للبيانات المهمة (15 دقيقة للـ profiles)
- Consistent performance للعمليات الأكثر أهمية

### ⚠️ التضحيات
- Feed endpoints ستكون أبطأ (direct DB queries)
- List endpoints بدون caching
- Search results بدون caching

**القرار**: هذه endpoints أقل أهمية من User profiles ومعلومات المنشور الواحد.

## 📈 الأولويات حسب الأهمية

1. **Critical (Must Cache)**: User Profiles - الأكثر طلباً
2. **High**: Single Post - مهم للـ engagement
3. **Medium**: Single Comment/Notification - حجم صغير
4. **Low (No Cache)**: Lists, Feeds, Search - حجم كبير/أقل أهمية

## ⚙️ Redis Configuration

```env
REDIS_ENDPOINT=redis-11950.c311.eu-central-1-1.ec2.cloud.redislabs.com:11950
REDIS_USERNAME=default
REDIS_PASSWORD=your-password
```

**Memory Limit**: 30 MB
**Eviction Policy**: allkeys-lru (يحذف الأقل استخداماً عند الامتلاء)
