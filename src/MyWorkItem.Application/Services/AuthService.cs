using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyWorkItem.Application.DTOs.Auth;
using MyWorkItem.Application.Interfaces;

namespace MyWorkItem.Application.Services;

/// <summary>
/// 認證服務：處理登入驗證與當前使用者查詢。
/// </summary>
public class AuthService(
    IAppDbContext context,
    IPasswordHasher hasher,
    ITokenService tokenService,
    ILogger<AuthService> logger)
{
    /// <summary>預先計算的 BCrypt 雜湊，用於使用者不存在時的固定時間比對，防止時序攻擊列舉帳號。</summary>
    private const string DummyHash = "$2a$11$K0ByBAEzSR.YEuxHbqMa5uNJtmxlOiJSBuVJtjmhKHZ1C7jUuvXim";

    /// <summary>驗證帳號密碼並回傳 JWT Token。</summary>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        // 無論使用者是否存在，皆執行 BCrypt.Verify 確保固定回應時間
        var hashToVerify = user?.PasswordHash ?? DummyHash;
        var isValid = hasher.Verify(request.Password, hashToVerify);

        if (user is null || !isValid)
        {
            logger.LogWarning("登入失敗：使用者 {Username}", username);
            return null;
        }

        logger.LogInformation("登入成功：使用者 {Username} (ID: {UserId})", user.Username, user.Id);

        return new LoginResponse
        {
            Token = tokenService.GenerateToken(user),
            User = new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Role = user.Role.ToString()
            }
        };
    }

    /// <summary>根據使用者 ID 查詢當前登入者資訊。</summary>
    public async Task<UserInfoDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await context.Users.FindAsync([userId], ct);
        if (user is null) return null;

        return new UserInfoDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString()
        };
    }
}
