using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyWorkItem.Infrastructure.Data.Configurations;

public class WorkItemConfiguration : IEntityTypeConfiguration<Domain.Entities.WorkItem>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.WorkItem> builder)
    {
        builder.ToTable("work_items");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(w => w.Title).HasMaxLength(500).IsRequired();
        builder.Property(w => w.Description).HasColumnType("text");

        builder.Property(w => w.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(w => w.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(w => w.Creator)
            .WithMany()
            .HasForeignKey(w => w.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
