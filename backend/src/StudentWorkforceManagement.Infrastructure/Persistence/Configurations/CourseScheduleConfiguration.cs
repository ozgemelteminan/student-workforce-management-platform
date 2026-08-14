using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class CourseScheduleConfiguration : IEntityTypeConfiguration<CourseSchedule>
{
    public void Configure(EntityTypeBuilder<CourseSchedule> builder)
    {
        builder.ToTable("CourseSchedules", table =>
        {
            table.HasCheckConstraint("CK_CourseSchedules_TimeRange", "\"EndTime\" > \"StartTime\"");
        });
        builder.ConfigureAuditableEntity();
        builder.HasQueryFilter(entity => entity.Student!.DeletedAt == null);
        builder.Property(entity => entity.CourseName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.CourseCode).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.DayOfWeek).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Location).HasMaxLength(200);
        builder.HasIndex(entity => entity.StudentId);
        builder.HasIndex(entity => entity.SemesterId);
        builder.HasIndex(entity => new { entity.StudentId, entity.SemesterId, entity.DayOfWeek });
        builder.HasOne(entity => entity.Student)
            .WithMany(student => student.CourseSchedules)
            .HasForeignKey(entity => entity.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Semester)
            .WithMany(semester => semester.CourseSchedules)
            .HasForeignKey(entity => entity.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
