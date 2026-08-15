using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class SemesterConfiguration : IEntityTypeConfiguration<Semester>
{
    public void Configure(EntityTypeBuilder<Semester> builder)
    {
        builder.ToTable("Semesters", table =>
        {
            table.HasCheckConstraint("CK_Semesters_DateRange", "\"EndDate\" >= \"StartDate\"");
        });
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.Name).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.IsActive).HasDefaultValue(true).IsRequired();
        builder.HasIndex(entity => entity.Name).IsUnique();
        builder.HasIndex(entity => entity.IsActive);
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.Status)
            .IsUnique()
            .HasFilter("\"Status\" = 'ACTIVE'");
    }
}
