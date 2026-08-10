using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskConfiguration : IEntityTypeConfiguration<DomainTask>
{
    public void Configure(EntityTypeBuilder<DomainTask> builder)
    {
        builder.ToTable("Tasks", table =>
        {
            table.HasCheckConstraint("CK_Tasks_EstimatedDurationMinutes", "\"EstimatedDurationMinutes\" >= 0");
        });
        builder.ConfigureAuditableEntity();
        builder.ConfigureSoftDelete();
        builder.ConfigureConcurrencyToken();
        builder.HasQueryFilter(entity => entity.DeletedAt == null);
        builder.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(8000);
        builder.Property(entity => entity.Priority).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Difficulty).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.CancellationReason).HasMaxLength(1000);
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.Priority);
        builder.HasIndex(entity => entity.Deadline);
        builder.HasIndex(entity => entity.CategoryId);
        builder.HasIndex(entity => entity.SemesterId);
        builder.HasIndex(entity => entity.AssignedStudentId);
        builder.HasOne(entity => entity.Category)
            .WithMany(category => category.Tasks)
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Semester)
            .WithMany(semester => semester.Tasks)
            .HasForeignKey(entity => entity.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.CreatedBy)
            .WithMany()
            .HasForeignKey(entity => entity.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.AssignedStudent)
            .WithMany()
            .HasForeignKey(entity => entity.AssignedStudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
