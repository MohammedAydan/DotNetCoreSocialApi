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
        Task<Comment> AddCommentAsync(Comment comment);
        Task<Comment> AddReplyAsync(Comment reply);

        Task<Comment> GetCommentByIdAsync(string commentId);
        Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(string postId, int page = 1, int limit = 20);
        Task<IEnumerable<Comment>> GetReplyCommentsByParentCommentIdAsync(string parentId, int page = 1, int limit = 20);

        Task<Comment> UpdateCommentAsync(Comment comment, string userId);
        
        Task<bool> DeleteCommentAsync(string commentId, string userId);
    }
}
