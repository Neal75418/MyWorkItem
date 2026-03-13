using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyWorkItem.Application.DTOs.WorkItems;
using MyWorkItem.Application.Interfaces;

namespace MyWorkItem.Application.Services;

/// <summary>
/// 管理員工作項目服務：CRUD 操作。
/// </summary>
public class AdminWorkItemService(IAppDbContext context, ILogger<AdminWorkItemService> logger)
{
    /// <summary>查詢所有工作項目（含建立者），依建立時間降冪排序。</summary>
    public async Task<List<AdminWorkItemDto>> GetAllWorkItemsAsync(CancellationToken ct = default)
    {
        var items = await context.WorkItems
            .Include(wi => wi.Creator)
            .OrderByDescending(wi => wi.CreatedAt)
            .ToListAsync(ct);

        return items.Select(wi => new AdminWorkItemDto
        {
            Id = wi.Id,
            Title = wi.Title,
            Description = wi.Description,
            CreatedByName = wi.Creator.DisplayName,
            CreatedAt = wi.CreatedAt,
            UpdatedAt = wi.UpdatedAt
        }).ToList();
    }

    /// <summary>建立工作項目；回傳建立後的 DTO，若建立者不存在則回傳 null。</summary>
    public async Task<AdminWorkItemDto?> CreateWorkItemAsync(
        CreateWorkItemRequest request, Guid createdBy, CancellationToken ct = default)
    {
        var creator = await context.Users.FindAsync([createdBy], ct);
        if (creator is null) return null;

        var now = DateTime.UtcNow;
        var workItem = new Domain.Entities.WorkItem
        {
            Title = request.Title,
            Description = request.Description,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.WorkItems.Add(workItem);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("管理員 {AdminId} 建立工作項目 {WorkItemId}：{Title}", createdBy, workItem.Id, workItem.Title);

        return new AdminWorkItemDto
        {
            Id = workItem.Id,
            Title = workItem.Title,
            Description = workItem.Description,
            CreatedByName = creator.DisplayName,
            CreatedAt = workItem.CreatedAt,
            UpdatedAt = workItem.UpdatedAt
        };
    }

    /// <summary>更新工作項目標題與描述。</summary>
    public async Task<AdminWorkItemDto?> UpdateWorkItemAsync(
        Guid id, UpdateWorkItemRequest request, CancellationToken ct = default)
    {
        var workItem = await context.WorkItems
            .Include(wi => wi.Creator)
            .FirstOrDefaultAsync(wi => wi.Id == id, ct);

        if (workItem is null) return null;

        workItem.Title = request.Title;
        workItem.Description = request.Description;
        workItem.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("工作項目 {WorkItemId} 已更新：{Title}", id, request.Title);

        return new AdminWorkItemDto
        {
            Id = workItem.Id,
            Title = workItem.Title,
            Description = workItem.Description,
            CreatedByName = workItem.Creator.DisplayName,
            CreatedAt = workItem.CreatedAt,
            UpdatedAt = workItem.UpdatedAt
        };
    }

    /// <summary>刪除工作項目。</summary>
    public async Task<bool> DeleteWorkItemAsync(Guid id, CancellationToken ct = default)
    {
        var workItem = await context.WorkItems.FindAsync([id], ct);
        if (workItem is null) return false;

        context.WorkItems.Remove(workItem);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("工作項目 {WorkItemId} 已刪除", id);
        return true;
    }
}
