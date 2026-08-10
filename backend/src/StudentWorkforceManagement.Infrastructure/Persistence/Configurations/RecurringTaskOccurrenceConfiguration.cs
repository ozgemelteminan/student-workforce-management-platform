using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class RecurringTaskOccurrenceConfiguration : IEntityTypeConfiguration<RecurringTaskOccurrence>
{
    public void Configure(EntityTypeBuilder<RecurringTaskOccurrence> builder)
    {
        builder.ToTable("RecurringTaskOccurrences", table =>
        {
            table.HasCheckConstraint("CK_RecurringTaskOccurrences_Attempts", "\"Attempts\" >= 0");
        });
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.FailureReason).HasMaxLength(1000);
        builder.HasIndex(entity => new { entity.RecurringTaskId, entity.ScheduledRunAt }).IsUnique();
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.GeneratedTaskId);
        builder.HasOne(entity => entity.RecurringTask)
            .WithMany(recurring => recurring.Occurrences)
            .HasForeignKey(entity => entity.RecurringTaskId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.GeneratedTask)
            .WithMany()
            .HasForeignKey(entity => entity.GeneratedTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
