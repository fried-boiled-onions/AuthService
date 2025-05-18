using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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

        public string GenerateAccessToken(string email)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.Name, email),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  
            new Claim("iat", DateTime.UtcNow.ToString())  
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
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public void SaveRefreshToken(string email, string refreshToken)
        {
            RefreshTokens[email] = refreshToken;
        }

        public bool ValidateRefreshToken(string email, string refreshToken)
        {
            return RefreshTokens.TryGetValue(email, out var savedToken) && savedToken == refreshToken;
        }

        public string? GetEmailFromExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_settings.Secret);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _settings.Issuer,
                    ValidAudience = _settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = false
                }, out _);

                return principal.Identity?.Name;
            }
            catch
            {
                return null;
            }
        }
    }
}
