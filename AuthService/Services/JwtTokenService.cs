using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AuthService.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services
{
    public class JwtTokenService
    {
        private readonly JwtSettings _settings;

        private static readonly Dictionary<string, string> RefreshTokens = new();

        public JwtTokenService(JwtSettings settings)
        {
            _settings = settings;
        }

        public string GenerateAccessToken(int id)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString();
        }

        public async Task SaveRefreshToken(int id, string refreshToken)
        {
            //var db = _redis.GetDatabase();
            //await db.StringSetValue(id.ToString(), refreshToken);
        }

        public bool VerifyAccessToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _settings.Issuer,
                    ValidAudience = _settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret))
                }, out _);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validate access token exception: {ex.Message}");
                return false;
            }
        }

        public bool ValidateRefreshToken(string email, string refreshToken)
        {

            return RefreshTokens.TryGetValue(email, out var savedToken) && savedToken == refreshToken;
        }

        public ClaimsPrincipal GetPrincipal(string token)
        {
            Console.WriteLine($"Secret: {_settings.Secret}, Issuer: {_settings.Issuer}, Audience: {_settings.Audience}");
            var tokenValidationParams = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParams, out var securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
                return principal;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation error: {ex.Message}");
                return null;
            }
        }
    }
}
