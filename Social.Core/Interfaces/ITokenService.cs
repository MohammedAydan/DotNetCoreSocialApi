using Social.Core.Entities;
using System.Collections.Generic;

namespace Social.Core.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user, IList<string> roles);
        string GenerateRefreshToken();
        Task<bool> IsTokenBlacklistedAsync(string token);
        Task BlacklistTokenAsync(string token, TimeSpan expiration);
    }
}