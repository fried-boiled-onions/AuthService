using Microsoft.AspNetCore.Mvc;
using AuthService.Models;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {

        private static readonly List<User> Users = new List<User>
        {
            new User { Id = 1, Email = "admin@example.com", PasswordHash = HashPassword("password") },
            new User { Id = 2, Email = "user@example.com", PasswordHash = HashPassword("12345") }
        };

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = Users.FirstOrDefault(u => u.Email == request.Email);

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized("Неверный email или пароль.");
            }
            var response = new LoginResponse
            {
                AccessToken = "mocked-access-token",
                RefreshToken = "mocked-refresh-token"
            };

            return Ok(response);
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == storedHash;
        }
    }
}
