using System.ComponentModel.DataAnnotations;

namespace MyWorkItem.Application.DTOs.WorkItems;

/// <summary>批次確認請求：最多 100 筆。</summary>
public class ConfirmWorkItemsRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<Guid> WorkItemIds { get; set; } = [];
}
