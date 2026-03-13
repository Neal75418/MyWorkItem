using Microsoft.EntityFrameworkCore;
using MyWorkItem.Application.Interfaces;
using MyWorkItem.Domain.Entities;

namespace MyWorkItem.Infrastructure.Data;

/// <summary>
/// EF Core 資料庫上下文：管理 Users、WorkItems、UserWorkItemStatuses 三張表。
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<UserWorkItemStatus> UserWorkItemStatuses => Set<UserWorkItemStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
