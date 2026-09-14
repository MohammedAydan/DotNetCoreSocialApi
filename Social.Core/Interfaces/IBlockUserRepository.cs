using Social.Core.Entities;

namespace Social.Core.Interfaces
{
    public interface IBlockUserRepository
    {
        Task BlockUserAsync(string blockerId, string blockedId, CancellationToken cancellationToken = default);
        Task UnblockUserAsync(string blockerId, string blockedId, CancellationToken cancellationToken = default);
        Task<bool> IsUserBlockedAsync(string blockerId, string blockedId, CancellationToken cancellationToken = default);
        Task<IEnumerable<BlockUser>> GetBlockedUsersAsync(string blockerId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);
    }
}