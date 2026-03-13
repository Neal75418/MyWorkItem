namespace MyWorkItem.Domain.Entities;

public class WorkItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Creator { get; set; } = null!;
    public ICollection<UserWorkItemStatus> UserStatuses { get; set; } = [];
}
