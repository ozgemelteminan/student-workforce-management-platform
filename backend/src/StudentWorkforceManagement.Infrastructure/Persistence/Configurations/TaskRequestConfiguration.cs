using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskRequestConfiguration : IEntityTypeConfiguration<TaskRequest>
{
    public void Configure(EntityTypeBuilder<TaskRequest> builder)
    {
        builder.ToTable("TaskRequests");
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.ReviewerComment).HasMaxLength(2000);
        builder.HasIndex(entity => entity.TaskId);
        builder.HasIndex(entity => entity.RequestedById);
        builder.HasIndex(entity => entity.Type);
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.CreatedAt);
        builder.HasIndex(entity => new { entity.TaskId, entity.Type })
            .IsUnique()
            .HasFilter("\"Status\" = 'PENDING'");
        builder.HasOne(entity => entity.Task)
            .WithMany(task => task.Requests)
            .HasForeignKey(entity => entity.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.RequestedBy)
            .WithMany(student => student.Requests)
            .HasForeignKey(entity => entity.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.SuggestedStudent)
            .WithMany()
            .HasForeignKey(entity => entity.SuggestedStudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.ReviewedBy)
            .WithMany()
            .HasForeignKey(entity => entity.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
