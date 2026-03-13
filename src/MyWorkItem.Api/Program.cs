using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyWorkItem.Application.Interfaces;
using MyWorkItem.Application.Services;
using MyWorkItem.Infrastructure.Data;
using MyWorkItem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// === 資料庫 ===
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// === JWT 認證 ===
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("必須設定 Jwt:Key（請檢查 appsettings 或環境變數）");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

// === 依賴注入 ===
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WorkItemService>();
builder.Services.AddScoped<AdminWorkItemService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// === CORS（React 開發伺服器）===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// === 自動遷移 + 種子資料（僅限開發環境）===
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(scope.ServiceProvider);
    app.MapOpenApi();
}

// === 全域例外處理：回傳 ProblemDetails，避免洩漏堆疊追蹤 ===
app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async httpContext =>
    {
        var exception = httpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");
        logger.LogError(exception, "未處理的例外：{Path}", httpContext.Request.Path);

        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            title = "Internal Server Error",
            status = 500
        });
    });
});

if (app.Environment.IsDevelopment())
    app.UseCors("AllowReactDev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
