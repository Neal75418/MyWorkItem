using System.ComponentModel.DataAnnotations;

namespace MyWorkItem.Application.DTOs.WorkItems;

/// <summary>建立工作項目請求。</summary>
public class CreateWorkItemRequest
{
    [Required(ErrorMessage = "標題不得為空")]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string? Description { get; set; }
}
