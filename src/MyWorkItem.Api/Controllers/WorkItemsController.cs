using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWorkItem.Api.Extensions;
using MyWorkItem.Application.DTOs.WorkItems;
using MyWorkItem.Application.Services;

namespace MyWorkItem.Api.Controllers;

/// <summary>使用者工作項目 API：查詢、確認、取消確認。</summary>
[ApiController]
[Route("api/work-items")]
[Authorize]
public class WorkItemsController(WorkItemService workItemService) : ControllerBase
{
    /// <summary>查詢當前使用者的工作項目清單，支援排序。</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await workItemService.GetWorkItemsAsync(userId, sortBy, sortDir, ct);
        return Ok(result);
    }

    /// <summary>查詢單一工作項目詳情。</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await workItemService.GetWorkItemDetailAsync(id, userId, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>批次確認工作項目。</summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmItems(ConfirmWorkItemsRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var count = await workItemService.ConfirmWorkItemsAsync(userId, request.WorkItemIds, ct);
        return Ok(new { confirmedCount = count });
    }

    /// <summary>取消確認單一工作項目。</summary>
    [HttpPatch("{id:guid}/unconfirm")]
    public async Task<IActionResult> Unconfirm(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var success = await workItemService.UnconfirmWorkItemAsync(userId, id, ct);
        if (!success) return NotFound();
        return Ok(new { message = "Unconfirmed successfully" });
    }
}
