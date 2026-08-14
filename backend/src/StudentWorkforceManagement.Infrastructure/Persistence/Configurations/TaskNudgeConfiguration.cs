using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskNudgeConfiguration : IEntityTypeConfiguration<TaskNudge>
{
    public void Configure(EntityTypeBuilder<TaskNudge> builder)
    {
        builder.ToTable("TaskNudges");
        builder.ConfigureAuditableEntity();
        builder.HasQueryFilter(entity =>
            entity.Task!.DeletedAt == null &&
            entity.SenderStudent!.DeletedAt == null &&
            entity.RecipientStudent!.DeletedAt == null);
        builder.HasIndex(entity => new { entity.TaskId, entity.SenderStudentId, entity.RecipientStudentId, entity.SentAt });
        builder.HasOne(entity => entity.Task).WithMany(task => task.Nudges).HasForeignKey(entity => entity.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.SenderStudent).WithMany(student => student.SentNudges).HasForeignKey(entity => entity.SenderStudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.RecipientStudent).WithMany(student => student.ReceivedNudges).HasForeignKey(entity => entity.RecipientStudentId).OnDelete(DeleteBehavior.Restrict);
    }
}
