using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyWorkItem.Application.DTOs.Auth;
using MyWorkItem.Application.Interfaces;
using MyWorkItem.Application.Services;
using MyWorkItem.Domain.Entities;
using MyWorkItem.Domain.Enums;
using MyWorkItem.Infrastructure.Data;
using NSubstitute;

namespace MyWorkItem.Application.Tests;

/// <summary>
/// AuthService 單元測試：驗證登入邏輯、timing attack 防護、使用者查詢。
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _hasher = Substitute.For<IPasswordHasher>();
        _tokenService = Substitute.For<ITokenService>();

        _sut = new AuthService(
            _db, _hasher, _tokenService,
            NullLogger<AuthService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUserInfo()
    {
        // Arrange
        var user = SeedUser("admin", "hashed-pw", "Admin User", UserRole.Admin);
        _hasher.Verify("correct-password", "hashed-pw").Returns(true);
        _tokenService.GenerateToken(Arg.Any<User>()).Returns("jwt-token-123");

        // Act
        var result = await _sut.LoginAsync(new LoginRequest
        {
            Username = "admin",
            Password = "correct-password"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("jwt-token-123", result.Token);
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal("admin", result.User.Username);
        Assert.Equal("Admin User", result.User.DisplayName);
        Assert.Equal("Admin", result.User.Role);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        SeedUser("admin", "hashed-pw");
        _hasher.Verify("wrong-password", "hashed-pw").Returns(false);

        // Act
        var result = await _sut.LoginAsync(new LoginRequest
        {
            Username = "admin",
            Password = "wrong-password"
        });

        // Assert
        Assert.Null(result);
        _tokenService.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_StillVerifiesHash_PreventingTimingAttack()
    {
        // Arrange — 資料庫無任何使用者
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        // Act
        var result = await _sut.LoginAsync(new LoginRequest
        {
            Username = "ghost",
            Password = "any-password"
        });

        // Assert — 即使使用者不存在，仍會呼叫 Verify（使用 DummyHash）防止 timing attack
        Assert.Null(result);
        _hasher.Received(1).Verify("any-password", Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_TrimsUsernameWhitespace()
    {
        // Arrange
        SeedUser("admin", "hashed-pw");
        _hasher.Verify("pw", "hashed-pw").Returns(true);
        _tokenService.GenerateToken(Arg.Any<User>()).Returns("token");

        // Act — 帳號前後有空白
        var result = await _sut.LoginAsync(new LoginRequest
        {
            Username = "  admin  ",
            Password = "pw"
        });

        // Assert — Trim 後應能找到使用者
        Assert.NotNull(result);
        Assert.Equal("admin", result.User.Username);
    }

    #endregion

    #region GetCurrentUserAsync

    [Fact]
    public async Task GetCurrentUserAsync_ExistingUser_ReturnsDto()
    {
        // Arrange
        var user = SeedUser("alice", "hash", "Alice", UserRole.User);

        // Act
        var result = await _sut.GetCurrentUserAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("alice", result.Username);
        Assert.Equal("Alice", result.DisplayName);
        Assert.Equal("User", result.Role);
    }

    [Fact]
    public async Task GetCurrentUserAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _sut.GetCurrentUserAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Helpers

    private User SeedUser(
        string username, string passwordHash,
        string displayName = "Test User", UserRole role = UserRole.User)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = passwordHash,
            DisplayName = displayName,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    #endregion
}
