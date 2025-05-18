using AuthService.Models;
using AuthService.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы в контейнер
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Настраиваем JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddSingleton<JwtTokenService>(provider =>
{
    var jwtSettings = provider.GetRequiredService<IOptions<JwtSettings>>().Value;
    return new JwtTokenService(jwtSettings);
});






builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CookieService>();

var app = builder.Build();

// Настраиваем конвейер обработки HTTP-запросов
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();