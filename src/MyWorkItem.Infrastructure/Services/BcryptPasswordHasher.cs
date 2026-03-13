using MyWorkItem.Application.Interfaces;

namespace MyWorkItem.Infrastructure.Services;

/// <summary>
/// BCrypt 密碼雜湊實作。
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
