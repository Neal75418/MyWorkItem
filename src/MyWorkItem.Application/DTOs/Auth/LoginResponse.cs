namespace MyWorkItem.Application.DTOs.Auth;

/// <summary>登入回應：包含 JWT Token 與使用者資訊。</summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserInfoDto User { get; set; } = null!;
}

/// <summary>使用者基本資訊 DTO。</summary>
public class UserInfoDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
