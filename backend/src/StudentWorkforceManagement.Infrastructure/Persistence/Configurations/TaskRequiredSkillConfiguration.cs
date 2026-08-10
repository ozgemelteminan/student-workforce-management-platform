using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskRequiredSkillConfiguration : IEntityTypeConfiguration<TaskRequiredSkill>
{
    public void Configure(EntityTypeBuilder<TaskRequiredSkill> builder)
    {
        builder.ToTable("TaskRequiredSkills");
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.MinimumLevel).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => entity.TaskId);
        builder.HasIndex(entity => new { entity.TaskId, entity.SkillId }).IsUnique();
        builder.HasOne(entity => entity.Task)
            .WithMany(task => task.RequiredSkills)
            .HasForeignKey(entity => entity.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Skill)
            .WithMany(skill => skill.TaskRequiredSkills)
            .HasForeignKey(entity => entity.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
