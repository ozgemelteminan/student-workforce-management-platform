using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable("Availability", table =>
        {
            table.HasCheckConstraint("CK_Availability_TimeRange", "\"EndTime\" > \"StartTime\"");
        });
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.DayOfWeek).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.HasIndex(entity => entity.StudentId);
        builder.HasIndex(entity => entity.SemesterId);
        builder.HasIndex(entity => new { entity.StudentId, entity.SemesterId, entity.DayOfWeek });
        builder.HasOne(entity => entity.Student)
            .WithMany(student => student.Availability)
            .HasForeignKey(entity => entity.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Semester)
            .WithMany(semester => semester.Availability)
            .HasForeignKey(entity => entity.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
