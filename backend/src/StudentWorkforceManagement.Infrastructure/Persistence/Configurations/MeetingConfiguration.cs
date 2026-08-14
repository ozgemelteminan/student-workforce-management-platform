using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("Meetings", table =>
        {
            table.HasCheckConstraint("CK_Meetings_ConfirmedRange", "\"ConfirmedEndAt\" IS NULL OR \"ConfirmedStartAt\" IS NULL OR \"ConfirmedEndAt\" > \"ConfirmedStartAt\"");
        });
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Location).HasMaxLength(500);
        builder.Property(entity => entity.Agenda).HasMaxLength(8000);
        builder.Property(entity => entity.Notes).HasMaxLength(12000);
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.ResponseDeadline);
        builder.HasOne(entity => entity.CreatedByUser).WithMany().HasForeignKey(entity => entity.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
