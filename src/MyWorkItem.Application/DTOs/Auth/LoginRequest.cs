using System.ComponentModel.DataAnnotations;

namespace MyWorkItem.Application.DTOs.Auth;

/// <summary>登入請求。</summary>
public class LoginRequest
{
    [Required, MaxLength(100)] public string Username { get; set; } = string.Empty;
    [Required, MaxLength(128)] public string Password { get; set; } = string.Empty;
}
