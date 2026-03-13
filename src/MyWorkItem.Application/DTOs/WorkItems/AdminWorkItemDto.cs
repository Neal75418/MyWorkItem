namespace MyWorkItem.Application.DTOs.WorkItems;

/// <summary>管理員端工作項目 DTO。</summary>
public class AdminWorkItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
