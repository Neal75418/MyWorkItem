namespace MyWorkItem.Application.DTOs.WorkItems;

/// <summary>工作項目清單回應：含項目清單與總筆數。</summary>
public class WorkItemListResponse
{
    public List<WorkItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
