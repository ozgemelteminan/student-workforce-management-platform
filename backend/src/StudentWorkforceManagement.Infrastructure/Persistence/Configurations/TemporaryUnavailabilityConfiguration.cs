using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TemporaryUnavailabilityConfiguration : IEntityTypeConfiguration<TemporaryUnavailability>
{
    public void Configure(EntityTypeBuilder<TemporaryUnavailability> builder)
    {
        builder.ToTable("TemporaryUnavailability", table =>
        {
            table.HasCheckConstraint("CK_TemporaryUnavailability_Range", "\"EndAt\" > \"StartAt\"");
        });
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.HasQueryFilter(entity => entity.Student!.DeletedAt == null);
        builder.Property(entity => entity.Category).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.Note).HasMaxLength(1000);
        builder.HasIndex(entity => new { entity.StudentId, entity.StartAt, entity.EndAt });
        builder.HasOne(entity => entity.Student).WithMany(student => student.TemporaryUnavailability).HasForeignKey(entity => entity.StudentId).OnDelete(DeleteBehavior.Restrict);
    }
}
