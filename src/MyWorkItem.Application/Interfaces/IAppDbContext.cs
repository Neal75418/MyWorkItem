using Microsoft.EntityFrameworkCore;
using MyWorkItem.Domain.Entities;

namespace MyWorkItem.Application.Interfaces;

/// <summary>
/// 應用層資料庫存取抽象，隔離 EF Core 實作細節。
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<WorkItem> WorkItems { get; }
    DbSet<UserWorkItemStatus> UserWorkItemStatuses { get; }

    /// <summary>儲存所有待變更至資料庫。</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
