using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskAssignmentHistoryConfiguration : IEntityTypeConfiguration<TaskAssignmentHistory>
{
    public void Configure(EntityTypeBuilder<TaskAssignmentHistory> builder)
    {
        builder.ToTable("TaskAssignmentHistory");
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Mode).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(1000);
        builder.HasIndex(entity => entity.TaskId);
        builder.HasIndex(entity => entity.StudentId);
        builder.HasIndex(entity => new { entity.TaskId, entity.IsActive });
        builder.HasIndex(entity => new { entity.TaskId, entity.StudentId })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");
        builder.ToTable("TaskAssignmentHistory", table =>
        {
            table.HasCheckConstraint("CK_TaskAssignmentHistory_PlannedEffortMinutes", "\"PlannedEffortMinutes\" IS NULL OR \"PlannedEffortMinutes\" >= 0");
        });
        builder.HasOne(entity => entity.Task)
            .WithMany(task => task.AssignmentHistory)
            .HasForeignKey(entity => entity.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Student)
            .WithMany(student => student.AssignmentHistory)
            .HasForeignKey(entity => entity.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.AssignedByUser)
            .WithMany()
            .HasForeignKey(entity => entity.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
