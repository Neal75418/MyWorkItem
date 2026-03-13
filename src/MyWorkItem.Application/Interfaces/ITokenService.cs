using MyWorkItem.Domain.Entities;

namespace MyWorkItem.Application.Interfaces;

/// <summary>
/// JWT 權杖產生服務。
/// </summary>
public interface ITokenService
{
    /// <summary>根據使用者資訊產生 JWT Bearer Token。</summary>
    string GenerateToken(User user);
}
