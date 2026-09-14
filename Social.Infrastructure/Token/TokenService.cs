using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Social.Infrastructure.Token
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly Social.Core.Interfaces.ICacheService _cacheService;

        public TokenService(IConfiguration config, Social.Core.Interfaces.ICacheService cacheService)
        {
            _config = config;
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public string GenerateToken(User user, IList<string> roles)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.NameId, user.UserName ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? "")
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var secretKey = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("JWT key is missing from configuration.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                //expires: DateTime.UtcNow.AddSeconds(20),
                expires: DateTime.UtcNow.AddMinutes(int.TryParse(_config["Jwt:ExpireTime"], out var minutes) ? minutes : 1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        static public void SaveToken(HttpContext context,string name, string token, DateTime? expires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expires ?? DateTime.UtcNow.AddMinutes(30)
            };

            context.Response.Cookies.Append(name, token, cookieOptions);
        }

        static public string? GetToken(HttpContext context, string name)
        {
            context.Request.Cookies.TryGetValue(name, out var token);
            return token;
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var key = $"blacklisted_token:{token}";
            return await _cacheService.ExistsAsync(key);
        }

        public async Task BlacklistTokenAsync(string token, TimeSpan expiration)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            var key = $"blacklisted_token:{token}";
            await _cacheService.SetAsync(key, true, expiration);
        }
    }
}
