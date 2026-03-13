namespace MyWorkItem.Domain.Entities;

public class UserWorkItemStatus
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid WorkItemId { get; set; }
    public bool IsConfirmed { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public User User { get; set; } = null!;
    public WorkItem WorkItem { get; set; } = null!;
}
