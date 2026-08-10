using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("Announcements");
        builder.ConfigureAuditableEntity();
        builder.ConfigureSoftDelete();
        builder.HasQueryFilter(entity => entity.DeletedAt == null);
        builder.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Content).HasMaxLength(12000).IsRequired();
        builder.HasIndex(entity => entity.IsPublished);
        builder.HasIndex(entity => entity.IsPinned);
        builder.HasIndex(entity => entity.ExpiresAt);
        builder.HasOne(entity => entity.CreatedBy)
            .WithMany()
            .HasForeignKey(entity => entity.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
