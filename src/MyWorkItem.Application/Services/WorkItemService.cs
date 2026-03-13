using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyWorkItem.Application.DTOs.WorkItems;
using MyWorkItem.Application.Interfaces;
using MyWorkItem.Domain.Entities;

namespace MyWorkItem.Application.Services;

/// <summary>
/// 工作項目服務：使用者端的查詢、確認與取消確認操作。
/// </summary>
public class WorkItemService(IAppDbContext context, ILogger<WorkItemService> logger)
{
    /// <summary>查詢使用者的工作項目清單，支援排序。</summary>
    public async Task<WorkItemListResponse> GetWorkItemsAsync(
        Guid userId, string? sortBy, string? sortDir, CancellationToken ct = default)
    {
        var query = context.WorkItems
            .Include(wi => wi.UserStatuses.Where(s => s.UserId == userId))
            .AsQueryable();

        // 排序：預設依建立時間降冪
        query = sortBy?.ToLower() switch
        {
            "createdat" => sortDir?.ToLower() == "asc"
                ? query.OrderBy(wi => wi.CreatedAt)
                : query.OrderByDescending(wi => wi.CreatedAt),
            "title" => sortDir?.ToLower() == "desc"
                ? query.OrderByDescending(wi => wi.Title)
                : query.OrderBy(wi => wi.Title),
            _ => query.OrderByDescending(wi => wi.CreatedAt)
        };

        var items = await query.ToListAsync(ct);

        return new WorkItemListResponse
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = items.Count
        };
    }

    /// <summary>查詢單一工作項目詳情（含建立者與確認狀態）。</summary>
    public async Task<WorkItemDetailDto?> GetWorkItemDetailAsync(
        Guid workItemId, Guid userId, CancellationToken ct = default)
    {
        var workItem = await context.WorkItems
            .Include(wi => wi.Creator)
            .Include(wi => wi.UserStatuses.Where(s => s.UserId == userId))
            .FirstOrDefaultAsync(wi => wi.Id == workItemId, ct);

        if (workItem is null) return null;

        var status = workItem.UserStatuses.FirstOrDefault();
        var isConfirmed = status?.IsConfirmed ?? false;

        return new WorkItemDetailDto
        {
            Id = workItem.Id,
            Title = workItem.Title,
            Description = workItem.Description,
            Status = isConfirmed ? "Confirmed" : "Pending",
            IsConfirmed = isConfirmed,
            ConfirmedAt = status?.ConfirmedAt,
            CreatedByName = workItem.Creator.DisplayName,
            CreatedAt = workItem.CreatedAt,
            UpdatedAt = workItem.UpdatedAt
        };
    }

    /// <summary>批次確認工作項目；回傳實際確認筆數。</summary>
    public async Task<int> ConfirmWorkItemsAsync(
        Guid userId, List<Guid> workItemIds, CancellationToken ct = default)
    {
        // 驗證所有請求 ID 確實存在，避免 FK 違規回傳 500
        var validIds = await context.WorkItems
            .Where(w => workItemIds.Contains(w.Id))
            .Select(w => w.Id)
            .ToListAsync(ct);

        if (validIds.Count == 0) return 0;

        var existingStatuses = await context.UserWorkItemStatuses
            .Where(s => s.UserId == userId && validIds.Contains(s.WorkItemId))
            .ToListAsync(ct);

        var existingMap = existingStatuses.ToDictionary(s => s.WorkItemId);
        var now = DateTime.UtcNow;

        foreach (var workItemId in validIds)
        {
            if (existingMap.TryGetValue(workItemId, out var status))
            {
                status.IsConfirmed = true;
                status.ConfirmedAt = now;
            }
            else
            {
                context.UserWorkItemStatuses.Add(new UserWorkItemStatus
                {
                    UserId = userId,
                    WorkItemId = workItemId,
                    IsConfirmed = true,
                    ConfirmedAt = now
                });
            }
        }

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // 並發 confirm 導致 unique constraint 衝突時，重新載入實際狀態
            logger.LogWarning("使用者 {UserId} 確認工作項目時發生並發衝突，重新計算", userId);
            var confirmedCount = await context.UserWorkItemStatuses
                .CountAsync(s => s.UserId == userId && validIds.Contains(s.WorkItemId) && s.IsConfirmed, ct);
            return confirmedCount;
        }

        logger.LogInformation("使用者 {UserId} 確認了 {Count} 筆工作項目", userId, validIds.Count);
        return validIds.Count;
    }

    /// <summary>取消確認單一工作項目。</summary>
    public async Task<bool> UnconfirmWorkItemAsync(
        Guid userId, Guid workItemId, CancellationToken ct = default)
    {
        var status = await context.UserWorkItemStatuses
            .FirstOrDefaultAsync(s => s.UserId == userId && s.WorkItemId == workItemId, ct);

        if (status is null || !status.IsConfirmed) return false;

        status.IsConfirmed = false;
        status.ConfirmedAt = null;
        await context.SaveChangesAsync(ct);
        logger.LogInformation("使用者 {UserId} 取消確認工作項目 {WorkItemId}", userId, workItemId);
        return true;
    }

    private static WorkItemDto MapToDto(Domain.Entities.WorkItem wi)
    {
        var status = wi.UserStatuses.FirstOrDefault();
        var isConfirmed = status?.IsConfirmed ?? false;

        return new WorkItemDto
        {
            Id = wi.Id,
            Title = wi.Title,
            Description = wi.Description,
            Status = isConfirmed ? "Confirmed" : "Pending",
            IsConfirmed = isConfirmed,
            ConfirmedAt = status?.ConfirmedAt,
            CreatedAt = wi.CreatedAt,
            UpdatedAt = wi.UpdatedAt
        };
    }
}
