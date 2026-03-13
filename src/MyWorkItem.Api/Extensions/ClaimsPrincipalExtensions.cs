using System.Security.Claims;

namespace MyWorkItem.Api.Extensions;

/// <summary>
/// ClaimsPrincipal 擴充方法：從 JWT Token 中擷取使用者資訊。
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>從 Token 的 NameIdentifier Claim 擷取使用者 ID。</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found");

        if (!Guid.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("Invalid user ID format in token");

        return userId;
    }
}
