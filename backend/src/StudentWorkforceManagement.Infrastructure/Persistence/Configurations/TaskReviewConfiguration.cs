using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskReviewConfiguration : IEntityTypeConfiguration<TaskReview>
{
    public void Configure(EntityTypeBuilder<TaskReview> builder)
    {
        builder.ToTable("TaskReviews");
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.ReviewerComment).HasMaxLength(2000);
        builder.HasIndex(entity => entity.TaskId);
        builder.HasIndex(entity => entity.SubmissionId);
        builder.HasIndex(entity => entity.ReviewedById);
        builder.HasOne(entity => entity.Task)
            .WithMany()
            .HasForeignKey(entity => entity.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Submission)
            .WithMany(submission => submission.Reviews)
            .HasForeignKey(entity => entity.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.ReviewedBy)
            .WithMany()
            .HasForeignKey(entity => entity.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
