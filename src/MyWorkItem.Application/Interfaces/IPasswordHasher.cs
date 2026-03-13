namespace MyWorkItem.Application.Interfaces;

/// <summary>
/// 密碼雜湊與驗證服務。
/// </summary>
public interface IPasswordHasher
{
    /// <summary>將明文密碼雜湊處理。</summary>
    string Hash(string password);

    /// <summary>驗證明文密碼是否與雜湊值相符。</summary>
    bool Verify(string password, string hash);
}
