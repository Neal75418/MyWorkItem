namespace MyWorkItem.Application.DTOs.WorkItems;

/// <summary>使用者端工作項目 DTO（詳情用，含建立者名稱）。</summary>
public class WorkItemDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsConfirmed { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
