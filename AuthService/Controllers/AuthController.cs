using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using AuthService.Models;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private static readonly List<User> Users = new List<User>();
        [HttpPost("register")]
        public IActionResult Register([FromBody] LoginRequest request)
        {
            var existingUser = Users.FirstOrDefault(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return Conflict("Пользователь с таким email уже существует.");
            }
            var hashedPassword = HashPassword(request.Password);
            var newUser = new User
            {
                Id = Users.Count + 1,
                Email = request.Email,
                PasswordHash = hashedPassword
            };
            Users.Add(newUser);

            return Ok("Пользователь успешно зарегистрирован.");
        }

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
