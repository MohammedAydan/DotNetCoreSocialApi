using System.Threading;
using System.Threading.Tasks;

namespace Social.Core.Interfaces
{
    public interface IDatabaseSeeder
    {
        Task SeedAsync(CancellationToken cancellationToken = default);
        Task<bool> EnsureAdminUserAsync(string email, string defaultPassword, CancellationToken cancellationToken = default);
        Task<bool> ResetAdminPasswordAsync(string email, string newPassword, CancellationToken cancellationToken = default);
    }
}
