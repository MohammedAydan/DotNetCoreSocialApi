using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Core.Interfaces
{
    public interface ICommentRepository
    {
        Task<Comment> AddCommentAsync(Comment comment, CancellationToken cancellationToken = default);
        Task<Comment> AddReplyAsync(Comment reply, CancellationToken cancellationToken = default);

        Task<Comment> GetCommentByIdAsync(string commentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(string postId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);
        Task<IEnumerable<Comment>> GetReplyCommentsByParentCommentIdAsync(string parentId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);

        Task<Comment> UpdateCommentAsync(Comment comment, string userId, CancellationToken cancellationToken = default);
        
        Task<bool> DeleteCommentAsync(string commentId, string userId, CancellationToken cancellationToken = default);
    }
}
