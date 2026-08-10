using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedback", table =>
        {
            table.HasCheckConstraint("CK_Feedback_Rating", "\"Rating\" IS NULL OR (\"Rating\" >= 1 AND \"Rating\" <= 5)");
        });
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.Comment).HasMaxLength(4000);
        builder.HasIndex(entity => entity.TaskId);
        builder.HasIndex(entity => entity.StudentId);
        builder.HasOne(entity => entity.Task)
            .WithMany(task => task.Feedback)
            .HasForeignKey(entity => entity.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Student)
            .WithMany(student => student.FeedbackReceived)
            .HasForeignKey(entity => entity.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.CreatedBy)
            .WithMany()
            .HasForeignKey(entity => entity.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
