using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskSubmissionConfiguration : IEntityTypeConfiguration<TaskSubmission>
{
    public void Configure(EntityTypeBuilder<TaskSubmission> builder)
    {
        builder.ToTable("TaskSubmissions");
        builder.ConfigureAuditableEntity();
        builder.ConfigureSoftDelete();
        builder.ConfigureConcurrencyToken();
        builder.HasQueryFilter(entity => entity.DeletedAt == null && entity.Task!.DeletedAt == null && entity.SubmittedBy!.DeletedAt == null);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.HasIndex(entity => entity.TaskId);
        builder.HasIndex(entity => entity.SubmittedById);
        builder.HasOne(entity => entity.Task)
            .WithMany(task => task.Submissions)
            .HasForeignKey(entity => entity.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.SubmittedBy)
            .WithMany(student => student.Submissions)
            .HasForeignKey(entity => entity.SubmittedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
