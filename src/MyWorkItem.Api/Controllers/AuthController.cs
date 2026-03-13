using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWorkItem.Api.Extensions;
using MyWorkItem.Application.DTOs.Auth;
using MyWorkItem.Application.Services;

namespace MyWorkItem.Api.Controllers;

/// <summary>認證 API：登入與當前使用者資訊。</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>使用帳號密碼登入，回傳 JWT Token。</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        if (result is null)
            return Unauthorized(new { message = "Invalid username or password" });

        return Ok(result);
    }

    /// <summary>取得當前登入使用者資訊（需認證）。</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var user = await authService.GetCurrentUserAsync(userId, ct);
        if (user is null) return NotFound();
        return Ok(user);
    }
}
