using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskTemplateConfiguration : IEntityTypeConfiguration<TaskTemplate>
{
    public void Configure(EntityTypeBuilder<TaskTemplate> builder)
    {
        builder.ToTable("TaskTemplates", table =>
        {
            table.HasCheckConstraint("CK_TaskTemplates_EstimatedDurationMinutes", "\"EstimatedDurationMinutes\" >= 0");
        });
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(8000);
        builder.Property(entity => entity.DefaultPriority).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.DefaultDifficulty).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ChecklistTemplateJson).HasColumnType("jsonb");
        builder.Property(entity => entity.RequiredSkillsTemplateJson).HasColumnType("jsonb");
        builder.HasIndex(entity => entity.CategoryId);
        builder.HasOne(entity => entity.Category)
            .WithMany(category => category.TaskTemplates)
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.CreatedBy)
            .WithMany()
            .HasForeignKey(entity => entity.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
