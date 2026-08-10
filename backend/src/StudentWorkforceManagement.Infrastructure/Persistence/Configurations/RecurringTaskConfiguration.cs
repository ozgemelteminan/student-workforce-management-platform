using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class RecurringTaskConfiguration : IEntityTypeConfiguration<RecurringTask>
{
    public void Configure(EntityTypeBuilder<RecurringTask> builder)
    {
        builder.ToTable("RecurringTasks");
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.Frequency).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.TimeZoneId).HasMaxLength(120).IsRequired();
        builder.HasIndex(entity => entity.TemplateId);
        builder.HasIndex(entity => entity.IsActive);
        builder.HasIndex(entity => entity.NextRunAt);
        builder.HasOne(entity => entity.Template)
            .WithMany(template => template.RecurringTasks)
            .HasForeignKey(entity => entity.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.CreatedBy)
            .WithMany()
            .HasForeignKey(entity => entity.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
