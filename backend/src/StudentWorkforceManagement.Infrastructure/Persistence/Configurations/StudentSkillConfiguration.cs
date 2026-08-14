using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class StudentSkillConfiguration : IEntityTypeConfiguration<StudentSkill>
{
    public void Configure(EntityTypeBuilder<StudentSkill> builder)
    {
        builder.ToTable("StudentSkills");
        builder.ConfigureAuditableEntity();
        builder.HasQueryFilter(entity => entity.Student!.DeletedAt == null);
        builder.Property(entity => entity.Level).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => entity.StudentId);
        builder.HasIndex(entity => new { entity.StudentId, entity.SkillId }).IsUnique();
        builder.HasOne(entity => entity.Student)
            .WithMany(student => student.Skills)
            .HasForeignKey(entity => entity.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Skill)
            .WithMany(skill => skill.StudentSkills)
            .HasForeignKey(entity => entity.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
