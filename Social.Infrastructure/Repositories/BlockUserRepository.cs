using Microsoft.EntityFrameworkCore;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Infrastructure.Data;

namespace Social.Infrastructure.Repositories
{
    public class BlockUserRepository(ApplicationDbContext dbContext) : IBlockUserRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task BlockUserAsync(string blockerId, string blockedId, CancellationToken cancellationToken = default)
        {
            BlockUser blockUser = BlockUser.Create(
                userId: blockerId,
                blockedUserId: blockedId
            );
            await _dbContext.BlockUsers.AddAsync(blockUser, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UnblockUserAsync(string blockerId, string blockedId, CancellationToken cancellationToken = default)
        {
            var blockUser = await _dbContext.BlockUsers
                .FirstOrDefaultAsync(b => b.UserId == blockerId && b.BlockedUserId == blockedId, cancellationToken);

            if (blockUser == null)
            {
                return;
            }

            _dbContext.BlockUsers.Remove(blockUser);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<BlockUser>> GetBlockedUsersAsync(string blockerId, int page = 1, int limit = 20, CancellationToken cancellationToken = default)
        {
            var blockedUsers = await _dbContext.BlockUsers
                .Where(b => b.UserId == blockerId)
                .Include(b => b.BlockedUser)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return blockedUsers;
        }

        public async Task<bool> IsUserBlockedAsync(string blockerId, string blockedId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.BlockUsers
                .AnyAsync(b => b.UserId == blockerId && b.BlockedUserId == blockedId, cancellationToken);
        }
    }
}
