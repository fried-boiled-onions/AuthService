using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AuthService.Models;
using AuthService.Services;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly JwtTokenService _jwtTokenService;
        private readonly CookieService _cookieService;
        private static readonly List<User> Users = new List<User>();

        public AuthController(JwtTokenService jwtTokenService, CookieService cookieService)
        {
            _jwtTokenService = jwtTokenService;
            _cookieService = cookieService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            var existingUser = Users.FirstOrDefault(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return Conflict(new ErrorResponse
                {
                    Message = "Пользователь с таким email уже существует.",
                    StatusCode = StatusCodes.Status409Conflict
                });
            }

            var hashedPassword = HashPassword(request.Password);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var newUser = new User
            {
                Id = Users.Count + 1,
                Email = request.Email,
                PasswordHash = hashedPassword,
                RefreshToken = refreshToken,
                Username = request.Username 
            };

            Users.Add(newUser);

            var accessToken = _jwtTokenService.GenerateAccessToken(newUser.Email);

            _cookieService.SetTokenInCookie("accessToken", accessToken, DateTime.UtcNow.AddMinutes(15));
            _cookieService.SetTokenInCookie("refreshToken", refreshToken, DateTime.UtcNow.AddDays(7));

            return Ok(new RegisterResponse
            {
                Message = "Регистрация прошла успешно.",
                Username = newUser.Username,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            });

        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "Неверный email или пароль.",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            var accessToken = _jwtTokenService.GenerateAccessToken(user.Email);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;

            _cookieService.SetTokenInCookie("accessToken", accessToken, DateTime.UtcNow.AddMinutes(15));
            _cookieService.SetTokenInCookie("refreshToken", refreshToken, DateTime.UtcNow.AddDays(7));

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] RefreshRequest request)
        {
            var user = Users.FirstOrDefault(u => u.RefreshToken == request.RefreshToken);
            if (user == null)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "Неверный refresh токен.",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Email);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;

            _cookieService.SetTokenInCookie("accessToken", newAccessToken, DateTime.UtcNow.AddMinutes(15));
            _cookieService.SetTokenInCookie("refreshToken", newRefreshToken, DateTime.UtcNow.AddDays(7));

            return Ok(new RefreshResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }


        [HttpPost("logout")]
        public IActionResult Logout([FromBody] LogoutRequest request)
        {
            var user = Users.FirstOrDefault(u => u.RefreshToken == request.RefreshToken);
            if (user == null)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "Неверный или уже использованный refresh токен.",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            user.RefreshToken = null;

            _cookieService.RemoveTokenFromCookie("accessToken");
            _cookieService.RemoveTokenFromCookie("refreshToken");

            return Ok(new LogoutResponse
            {
                Message = "Вы успешно вышли из системы."
            });
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == storedHash;
        }
    }
}
