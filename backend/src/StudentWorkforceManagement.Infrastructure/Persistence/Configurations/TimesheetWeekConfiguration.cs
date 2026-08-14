using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TimesheetWeekConfiguration : IEntityTypeConfiguration<TimesheetWeek>
{
    public void Configure(EntityTypeBuilder<TimesheetWeek> builder)
    {
        builder.ToTable("TimesheetWeeks", table =>
        {
            table.HasCheckConstraint("CK_TimesheetWeeks_TargetMinutes", "\"TargetMinutes\" >= 0");
        });
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ReviewerComment).HasMaxLength(2000);
        builder.HasIndex(entity => new { entity.StudentId, entity.WeekStartDate }).IsUnique();
        builder.HasIndex(entity => entity.Status);
        builder.HasOne(entity => entity.Student).WithMany(student => student.TimesheetWeeks).HasForeignKey(entity => entity.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.ReviewedByUser).WithMany().HasForeignKey(entity => entity.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
