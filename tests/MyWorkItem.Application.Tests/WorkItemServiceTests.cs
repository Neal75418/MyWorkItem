using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyWorkItem.Application.Services;
using MyWorkItem.Domain.Entities;
using MyWorkItem.Domain.Enums;
using MyWorkItem.Infrastructure.Data;

namespace MyWorkItem.Application.Tests;

/// <summary>
/// WorkItemService 單元測試：驗證確認/取消確認邏輯、per-user 狀態隔離、排序。
/// </summary>
public class WorkItemServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly WorkItemService _sut;

    public WorkItemServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new WorkItemService(_db, NullLogger<WorkItemService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    #region GetWorkItemsAsync

    [Fact]
    public async Task GetWorkItemsAsync_ReturnsAllItems_WithUserSpecificStatus()
    {
        // Arrange
        var user = SeedUser();
        var wi1 = SeedWorkItem("Item A", user.Id);
        var wi2 = SeedWorkItem("Item B", user.Id);

        // User 只確認了 Item A
        SeedStatus(user.Id, wi1.Id, isConfirmed: true);

        // Act
        var result = await _sut.GetWorkItemsAsync(user.Id, null, null);

        // Assert — 兩筆都回傳，但狀態不同
        Assert.Equal(2, result.Items.Count);
        var itemA = result.Items.First(i => i.Title == "Item A");
        var itemB = result.Items.First(i => i.Title == "Item B");
        Assert.True(itemA.IsConfirmed);
        Assert.Equal("Confirmed", itemA.Status);
        Assert.False(itemB.IsConfirmed);
        Assert.Equal("Pending", itemB.Status);
    }

    [Fact]
    public async Task GetWorkItemsAsync_DefaultSortByCreatedAtDesc()
    {
        // Arrange
        var user = SeedUser();
        SeedWorkItem("Older", user.Id, createdAt: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SeedWorkItem("Newer", user.Id, createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        // Act — 不指定排序 → 預設 createdAt desc
        var result = await _sut.GetWorkItemsAsync(user.Id, null, null);

        // Assert
        Assert.Equal("Newer", result.Items[0].Title);
        Assert.Equal("Older", result.Items[1].Title);
    }

    [Fact]
    public async Task GetWorkItemsAsync_SortByTitleAsc()
    {
        // Arrange
        var user = SeedUser();
        SeedWorkItem("Banana", user.Id);
        SeedWorkItem("Apple", user.Id);

        // Act
        var result = await _sut.GetWorkItemsAsync(user.Id, "title", "asc");

        // Assert
        Assert.Equal("Apple", result.Items[0].Title);
        Assert.Equal("Banana", result.Items[1].Title);
    }

    #endregion

    #region ConfirmWorkItemsAsync

    [Fact]
    public async Task ConfirmWorkItemsAsync_CreatesNewStatusRecords()
    {
        // Arrange
        var user = SeedUser();
        var wi = SeedWorkItem("Task 1", user.Id);

        // Act — 首次確認，應建立新的 status record
        var count = await _sut.ConfirmWorkItemsAsync(user.Id, [wi.Id]);

        // Assert
        Assert.Equal(1, count);
        var status = await _db.UserWorkItemStatuses
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.WorkItemId == wi.Id);
        Assert.NotNull(status);
        Assert.True(status.IsConfirmed);
        Assert.NotNull(status.ConfirmedAt);
    }

    [Fact]
    public async Task ConfirmWorkItemsAsync_UpdatesExistingPendingStatus()
    {
        // Arrange — 已有一筆 Pending 狀態（之前 unconfirm 過）
        var user = SeedUser();
        var wi = SeedWorkItem("Task 1", user.Id);
        SeedStatus(user.Id, wi.Id, isConfirmed: false);

        // Act
        var count = await _sut.ConfirmWorkItemsAsync(user.Id, [wi.Id]);

        // Assert — 應更新現有記錄，而非新增
        Assert.Equal(1, count);
        var statuses = await _db.UserWorkItemStatuses
            .Where(s => s.UserId == user.Id && s.WorkItemId == wi.Id)
            .ToListAsync();
        Assert.Single(statuses);
        Assert.True(statuses[0].IsConfirmed);
    }

    [Fact]
    public async Task ConfirmWorkItemsAsync_InvalidIds_ReturnsZero()
    {
        // Arrange
        var user = SeedUser();

        // Act — 傳入不存在的 WorkItem ID
        var count = await _sut.ConfirmWorkItemsAsync(user.Id, [Guid.NewGuid()]);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ConfirmWorkItemsAsync_PerUserIsolation()
    {
        // Arrange — 兩個使用者，同一個 WorkItem
        var userA = SeedUser("alice");
        var userB = SeedUser("bob");
        var wi = SeedWorkItem("Shared Item", userA.Id);

        // Act — 只有 User A 確認
        await _sut.ConfirmWorkItemsAsync(userA.Id, [wi.Id]);

        // 清除 change tracker 模擬新的 HTTP request scope，避免 InMemory provider
        // 的 change tracker 干擾 filtered Include 結果
        _db.ChangeTracker.Clear();

        // Assert — User A 看到 Confirmed，User B 看到 Pending
        var resultA = await _sut.GetWorkItemsAsync(userA.Id, null, null);
        _db.ChangeTracker.Clear();
        var resultB = await _sut.GetWorkItemsAsync(userB.Id, null, null);

        Assert.True(resultA.Items[0].IsConfirmed);
        Assert.False(resultB.Items[0].IsConfirmed);
    }

    [Fact]
    public async Task ConfirmWorkItemsAsync_BatchConfirm_MultipleItems()
    {
        // Arrange
        var user = SeedUser();
        var wi1 = SeedWorkItem("Task 1", user.Id);
        var wi2 = SeedWorkItem("Task 2", user.Id);
        var wi3 = SeedWorkItem("Task 3", user.Id);

        // Act — 一次確認 3 筆
        var count = await _sut.ConfirmWorkItemsAsync(user.Id, [wi1.Id, wi2.Id, wi3.Id]);

        // Assert
        Assert.Equal(3, count);
        var confirmedCount = await _db.UserWorkItemStatuses
            .CountAsync(s => s.UserId == user.Id && s.IsConfirmed);
        Assert.Equal(3, confirmedCount);
    }

    #endregion

    #region UnconfirmWorkItemAsync

    [Fact]
    public async Task UnconfirmWorkItemAsync_ConfirmedItem_ReturnsTrueAndResetsStatus()
    {
        // Arrange
        var user = SeedUser();
        var wi = SeedWorkItem("Task 1", user.Id);
        SeedStatus(user.Id, wi.Id, isConfirmed: true);

        // Act
        var result = await _sut.UnconfirmWorkItemAsync(user.Id, wi.Id);

        // Assert
        Assert.True(result);
        var status = await _db.UserWorkItemStatuses
            .FirstAsync(s => s.UserId == user.Id && s.WorkItemId == wi.Id);
        Assert.False(status.IsConfirmed);
        Assert.Null(status.ConfirmedAt);
    }

    [Fact]
    public async Task UnconfirmWorkItemAsync_AlreadyPending_ReturnsFalse()
    {
        // Arrange — 已經是 Pending 狀態
        var user = SeedUser();
        var wi = SeedWorkItem("Task 1", user.Id);
        SeedStatus(user.Id, wi.Id, isConfirmed: false);

        // Act
        var result = await _sut.UnconfirmWorkItemAsync(user.Id, wi.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UnconfirmWorkItemAsync_NoStatusRecord_ReturnsFalse()
    {
        // Arrange — 從未確認過（無 status record）
        var user = SeedUser();
        var wi = SeedWorkItem("Task 1", user.Id);

        // Act
        var result = await _sut.UnconfirmWorkItemAsync(user.Id, wi.Id);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetWorkItemDetailAsync

    [Fact]
    public async Task GetWorkItemDetailAsync_ConfirmedItem_ReturnsDetailWithConfirmedStatus()
    {
        // Arrange
        var user = SeedUser("alice");
        var wi = SeedWorkItem("Important Task", user.Id);
        SeedStatus(user.Id, wi.Id, isConfirmed: true);
        _db.ChangeTracker.Clear();

        // Act
        var result = await _sut.GetWorkItemDetailAsync(wi.Id, user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(wi.Id, result.Id);
        Assert.Equal("Important Task", result.Title);
        Assert.True(result.IsConfirmed);
        Assert.Equal("Confirmed", result.Status);
        Assert.NotNull(result.ConfirmedAt);
        Assert.Equal("alice", result.CreatedByName);
    }

    [Fact]
    public async Task GetWorkItemDetailAsync_PendingItem_ReturnsDetailWithPendingStatus()
    {
        // Arrange — 未確認過（無 status record）
        var user = SeedUser();
        var wi = SeedWorkItem("Pending Task", user.Id);
        _db.ChangeTracker.Clear();

        // Act
        var result = await _sut.GetWorkItemDetailAsync(wi.Id, user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pending Task", result.Title);
        Assert.False(result.IsConfirmed);
        Assert.Equal("Pending", result.Status);
        Assert.Null(result.ConfirmedAt);
    }

    [Fact]
    public async Task GetWorkItemDetailAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var user = SeedUser();

        // Act
        var result = await _sut.GetWorkItemDetailAsync(Guid.NewGuid(), user.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Helpers

    private User SeedUser(string username = "testuser")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = "irrelevant",
            DisplayName = username,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    private Domain.Entities.WorkItem SeedWorkItem(
        string title, Guid createdBy, DateTime? createdAt = null)
    {
        var now = createdAt ?? DateTime.UtcNow;
        var wi = new Domain.Entities.WorkItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.WorkItems.Add(wi);
        _db.SaveChanges();
        return wi;
    }

    private void SeedStatus(Guid userId, Guid workItemId, bool isConfirmed)
    {
        _db.UserWorkItemStatuses.Add(new UserWorkItemStatus
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WorkItemId = workItemId,
            IsConfirmed = isConfirmed,
            ConfirmedAt = isConfirmed ? DateTime.UtcNow : null
        });
        _db.SaveChanges();
    }

    #endregion
}
