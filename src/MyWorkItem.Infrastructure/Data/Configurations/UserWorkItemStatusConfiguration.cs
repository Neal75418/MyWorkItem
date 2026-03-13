using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyWorkItem.Domain.Entities;

namespace MyWorkItem.Infrastructure.Data.Configurations;

public class UserWorkItemStatusConfiguration : IEntityTypeConfiguration<UserWorkItemStatus>
{
    public void Configure(EntityTypeBuilder<UserWorkItemStatus> builder)
    {
        builder.ToTable("user_work_item_statuses");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(s => new { s.UserId, s.WorkItemId }).IsUnique();
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.WorkItemId);

        builder.HasOne(s => s.User)
            .WithMany(u => u.WorkItemStatuses)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.WorkItem)
            .WithMany(w => w.UserStatuses)
            .HasForeignKey(s => s.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
