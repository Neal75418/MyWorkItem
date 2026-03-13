using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWorkItem.Api.Extensions;
using MyWorkItem.Application.DTOs.WorkItems;
using MyWorkItem.Application.Services;

namespace MyWorkItem.Api.Controllers;

/// <summary>管理員工作項目 API：CRUD 操作（僅限 Admin 角色）。</summary>
[ApiController]
[Route("api/admin/work-items")]
[Authorize(Roles = "Admin")]
public class AdminWorkItemsController(AdminWorkItemService adminService) : ControllerBase
{
    /// <summary>查詢所有工作項目。</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await adminService.GetAllWorkItemsAsync(ct);
        return Ok(items);
    }

    /// <summary>建立新工作項目。</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkItemRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await adminService.CreateWorkItemAsync(request, userId, ct);
        if (result is null) return BadRequest(new { message = "Creator user not found" });
        return Created($"/api/admin/work-items/{result.Id}", result);
    }

    /// <summary>更新工作項目。</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateWorkItemRequest request, CancellationToken ct)
    {
        var result = await adminService.UpdateWorkItemAsync(id, request, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>刪除工作項目。</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await adminService.DeleteWorkItemAsync(id, ct);
        if (!success) return NotFound();
        return NoContent();
    }
}
