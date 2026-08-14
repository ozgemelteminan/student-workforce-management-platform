using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskChecklistItemConfiguration : IEntityTypeConfiguration<TaskChecklistItem>
{
    public void Configure(EntityTypeBuilder<TaskChecklistItem> builder)
    {
        builder.ToTable("TaskChecklistItems", table =>
        {
            table.HasCheckConstraint("CK_TaskChecklistItems_Order", "\"Order\" >= 0");
        });
        builder.ConfigureAuditableEntity();
        builder.HasQueryFilter(entity => entity.Task!.DeletedAt == null);
        builder.Property(entity => entity.Title).HasMaxLength(300).IsRequired();
        builder.HasIndex(entity => entity.TaskId);
        builder.HasIndex(entity => new { entity.TaskId, entity.Order });
        builder.HasOne(entity => entity.Task)
            .WithMany(task => task.ChecklistItems)
            .HasForeignKey(entity => entity.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.CompletedBy)
            .WithMany()
            .HasForeignKey(entity => entity.CompletedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
