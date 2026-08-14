using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("TaskDependencies", table =>
        {
            table.HasCheckConstraint("CK_TaskDependencies_NoSelfDependency", "\"TaskId\" <> \"DependsOnTaskId\"");
        });
        builder.ConfigureAuditableEntity();
        builder.HasQueryFilter(entity => entity.Task!.DeletedAt == null && entity.DependsOnTask!.DeletedAt == null);
        builder.HasIndex(entity => entity.TaskId);
        builder.HasIndex(entity => new { entity.TaskId, entity.DependsOnTaskId }).IsUnique();
        builder.HasOne(entity => entity.Task)
            .WithMany(task => task.Dependencies)
            .HasForeignKey(entity => entity.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.DependsOnTask)
            .WithMany(task => task.DependentTasks)
            .HasForeignKey(entity => entity.DependsOnTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
