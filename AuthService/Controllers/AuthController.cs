using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StackExchange.Redis;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using AuthService.Models;
using AuthService.Services;
using AuthService.Data;

namespace AuthService.Controllers;

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly JwtTokenService _jwtTokenService;
        private readonly CookieService _cookieService;
        private static readonly List<User> Users = new List<User>();
        private readonly UserRepository _repository = new UserRepository("Host=localhost;Port=5432;Database=messenger-db;Username=postgres;Password=postgres");
        private readonly IConnectionMultiplexer _redis;

    public AuthController(JwtTokenService jwtTokenService, CookieService cookieService, IConnectionMultiplexer redis)
    {
        _jwtTokenService = jwtTokenService;
        _cookieService = cookieService;
        //_repository = repository;
        _redis = redis;
    }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var hashedPassword = HashPassword(request.Password);

                var userId = await _repository.AddUserAsync(request.Username, request.Email, hashedPassword);

                var accessToken = _jwtTokenService.GenerateAccessToken(userId);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();

                var db = _redis.GetDatabase();
                await db.StringSetAsync(Convert.ToString(userId), refreshToken, TimeSpan.FromDays(30));

                _cookieService.SetTokenInCookie("accessToken", accessToken, DateTime.UtcNow.AddMinutes(15));
                _cookieService.SetTokenInCookie("refreshToken", refreshToken, DateTime.UtcNow.AddDays(30));

                return Ok(new RegisterResponse
            {
                Message = "Регистрация прошла успешно",
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return Conflict(new ErrorResponse
                {
                    Message = ex.Message,
                    StatusCode = StatusCodes.Status409Conflict
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = $"UnexpectedError: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
        var userId = await _repository.LoginUserAsync(request.Email, HashPassword(request.Password));
            if (userId == -1)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "Неверный email или пароль.",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            var accessToken = _jwtTokenService.GenerateAccessToken(userId);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var db = _redis.GetDatabase();
            await db.StringSetAsync(Convert.ToString(userId), refreshToken, TimeSpan.FromDays(30));

            _cookieService.SetTokenInCookie("accessToken", accessToken, DateTime.UtcNow.AddMinutes(15));
            _cookieService.SetTokenInCookie("refreshToken", refreshToken, DateTime.UtcNow.AddDays(30));

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var accessToken = request.AccessToken;
        var refreshToken = request.RefreshToken;
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "Неверный refresh токен.",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        var principal = _jwtTokenService.GetPrincipal(accessToken);
        if (principal == null)
        {
            Console.WriteLine("У нас principal не достается");
            return Unauthorized(new ErrorResponse
            {
                Message = "Неверный access токен.",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("У нас UserID не парсится");
            return Unauthorized(new ErrorResponse
            {
                Message = "Неверный access токен.",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        var db = _redis.GetDatabase();
        var storedRefreshToken = await db.StringGetAsync(Convert.ToString(userId));
        if (storedRefreshToken != refreshToken)
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "Неверный refresh токен.",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        var newAccessToken = _jwtTokenService.GenerateAccessToken(int.Parse(userId));
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        await db.StringSetAsync(userId, newRefreshToken, TimeSpan.FromDays(30));

        _cookieService.SetTokenInCookie("accessToken", newAccessToken, DateTime.UtcNow.AddMinutes(15));
        _cookieService.SetTokenInCookie("refreshToken", newRefreshToken, DateTime.UtcNow.AddDays(30));

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

    [HttpGet("verify")]
    public IActionResult VerifyToken()
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (_jwtTokenService.VerifyAccessToken(token))
        {
            return Ok();
        }
        return Unauthorized();
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
