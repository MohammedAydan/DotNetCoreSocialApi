using Microsoft.EntityFrameworkCore;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Infrastucture.Data;
using Social.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Social.Infrastucture.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationRepository _notificationRepository;

        public CommentRepository(ApplicationDbContext dbContext, INotificationRepository notificationRepository)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        }

        public async Task<Comment> AddCommentAsync(Comment comment)
        {
            if (comment == null) throw new ArgumentNullException(nameof(comment));
            if (string.IsNullOrEmpty(comment.PostId)) throw new ArgumentException("Post ID cannot be null or empty.", nameof(comment));
            if (string.IsNullOrEmpty(comment.UserId)) throw new ArgumentException("User ID cannot be null or empty.", nameof(comment));
            if (string.IsNullOrEmpty(comment.Content)) throw new ArgumentException("Comment content cannot be null or empty.", nameof(comment));

            comment.Id = Guid.NewGuid().ToString();
            comment.CreatedAt = DateTime.UtcNow;

            _dbContext.Comments.Add(comment);

            // Increment Post.CommentsCount
            var post = await _dbContext.Posts.FindAsync(comment.PostId);
            if (post != null)
            {
                post.CommentsCount = Math.Max(0, post.CommentsCount + 1);
                _dbContext.Posts.Update(post);

                // Create notification for post comment (only if commenter is not the post owner)
                if (post.UserId != comment.UserId)
                {
                    await CreateCommentNotificationAsync(comment, post.UserId, MediaTypes.Actions.Comment);
                }
            }

            await _dbContext.SaveChangesAsync();
            return comment;
        }

        public async Task<Comment> AddReplyAsync(Comment reply)
        {
            if (reply == null) throw new ArgumentNullException(nameof(reply));
            if (string.IsNullOrEmpty(reply.ParentId)) throw new ArgumentException("Parent ID cannot be null or empty.", nameof(reply));
            if (string.IsNullOrEmpty(reply.PostId)) throw new ArgumentException("Post ID cannot be null or empty.", nameof(reply));
            if (string.IsNullOrEmpty(reply.UserId)) throw new ArgumentException("User ID cannot be null or empty.", nameof(reply));
            if (string.IsNullOrEmpty(reply.Content)) throw new ArgumentException("Comment content cannot be null or empty.", nameof(reply));

            reply.Id = Guid.NewGuid().ToString();
            reply.CreatedAt = DateTime.UtcNow;

            _dbContext.Comments.Add(reply);

            // Increment parent.RepliesCount
            var parent = await _dbContext.Comments.FindAsync(reply.ParentId);
            if (parent != null)
            {
                parent.RepliesCount = Math.Max(0, parent.RepliesCount + 1);
                _dbContext.Comments.Update(parent);

                // Create notification for reply (only if replier is not the parent comment owner)
                if (parent.UserId != reply.UserId)
                {
                    await CreateCommentNotificationAsync(reply, parent.UserId, MediaTypes.Actions.CommentReply);
                }
            }

            await _dbContext.SaveChangesAsync();
            return reply;
        }

        public async Task<bool> DeleteCommentAsync(string commentId, string userId)
        {
            if (string.IsNullOrEmpty(commentId)) throw new ArgumentException("Comment ID cannot be null or empty.", nameof(commentId));

            var comment = await _dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null) throw new ArgumentException("Comment not found.", nameof(comment));
            if (comment.UserId != userId) throw new UnauthorizedAccessException("You are not authorized to delete this comment.");

            comment.IsDeleted = true;
            _dbContext.Comments.Update(comment);

            if (string.IsNullOrEmpty(comment.ParentId))
            {
                // Decrement Post.CommentsCount for top-level comment
                var post = await _dbContext.Posts.FindAsync(comment.PostId);
                if (post != null && post.CommentsCount > 0)
                {
                    post.CommentsCount = Math.Max(0, post.CommentsCount - 1);
                    _dbContext.Posts.Update(post);
                }

                // Delete comment notification
                var notification = await _notificationRepository.GetNotificationByQueryAsync(n => n.CommentId == commentId);
                if (notification != null)
                {
                    await _notificationRepository.DeleteAsync(notification.Id);
                }
            }
            else
            {
                // Decrement parent.RepliesCount for reply comment
                var parent = await _dbContext.Comments.FindAsync(comment.ParentId);
                if (parent != null && parent.RepliesCount > 0)
                {
                    parent.RepliesCount = Math.Max(0, parent.RepliesCount - 1);
                    _dbContext.Comments.Update(parent);
                }

                // Delete reply notification
                var notification = await _notificationRepository.GetNotificationByQueryAsync(n => n.CommentId == commentId);
                if (notification != null)
                {
                    await _notificationRepository.DeleteAsync(notification.Id);
                }
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<Comment> GetCommentByIdAsync(string commentId)
        {
            if (string.IsNullOrEmpty(commentId)) throw new ArgumentException("Comment ID cannot be null or empty.", nameof(commentId));

            var comment = await _dbContext.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null) throw new ArgumentException("Comment not found.", nameof(comment));
            return comment;
        }

        public async Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(string postId, int page = 1, int limit = 20)
        {
            if (string.IsNullOrEmpty(postId)) throw new ArgumentException("Post ID cannot be null or empty.", nameof(postId));
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page number must be greater than 0.");
            if (limit < 1) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than 0.");

            var comments = await _dbContext.Comments
                .AsNoTracking()
                .Where(c => c.PostId == postId && !c.IsDeleted && c.ParentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Include(c => c.User)
                .ToListAsync();

            return comments;
        }

        public async Task<IEnumerable<Comment>> GetReplyCommentsByParentCommentIdAsync(string parentId, int page = 1, int limit = 20)
        {
            if (string.IsNullOrEmpty(parentId)) throw new ArgumentException("Parent ID cannot be null or empty.", nameof(parentId));
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page number must be greater than 0.");
            if (limit < 1) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than 0.");

            return await _dbContext.Comments
                .AsNoTracking()
                .Where(c => c.ParentId == parentId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Include(c => c.User)
                .ToListAsync();
        }

        public async Task<Comment> UpdateCommentAsync(Comment comment, string userId)
        {
            if (comment == null) throw new ArgumentNullException(nameof(comment));
            if (string.IsNullOrEmpty(comment.Id)) throw new ArgumentException("Comment ID cannot be null or empty.", nameof(comment));
            if (string.IsNullOrEmpty(comment.Content)) throw new ArgumentException("Comment content cannot be null or empty.", nameof(comment));

            var existingComment = await _dbContext.Comments.FindAsync(comment.Id);
            if (existingComment == null) throw new ArgumentException("Comment not found.", nameof(comment));
            if (existingComment.UserId != userId) throw new UnauthorizedAccessException("You are not authorized to update this comment.");

            existingComment.Content = comment.Content;
            existingComment.UpdatedAt = DateTime.UtcNow;

            _dbContext.Comments.Update(existingComment);
            await _dbContext.SaveChangesAsync();
            return existingComment;
        }

        private async Task CreateCommentNotificationAsync(Comment comment, string recipientId, string notificationType)
        {
            string message;

            switch (notificationType)
            {
                case MediaTypes.Actions.Comment:
                    message = $"{comment.UserId} commented on your post.";
                    break;
                case MediaTypes.Actions.CommentReply:
                    message = $"{comment.UserId} replied to your comment.";
                    break;
                default:
                    return;
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                RecipientId = recipientId,
                UserId = comment.UserId,
                Type = notificationType,
                Message = message,
                PostId = comment.PostId,
                CommentId = comment.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
        }
    }
}