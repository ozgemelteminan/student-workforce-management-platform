using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class MarketplaceClaimConfiguration : IEntityTypeConfiguration<MarketplaceClaim>
{
    public void Configure(EntityTypeBuilder<MarketplaceClaim> builder)
    {
        builder.ToTable("MarketplaceClaims");
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.HasQueryFilter(entity => entity.MarketplaceListing!.Task!.DeletedAt == null && entity.Student!.DeletedAt == null);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => entity.MarketplaceListingId);
        builder.HasIndex(entity => entity.StudentId);
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.ExpiresAt);
        builder.HasIndex(entity => new { entity.MarketplaceListingId, entity.StudentId }).IsUnique();
        builder.HasIndex(entity => entity.MarketplaceListingId)
            .IsUnique()
            .HasFilter("\"Status\" IN ('PENDING', 'APPROVED')");
        builder.HasOne(entity => entity.MarketplaceListing)
            .WithMany(listing => listing.Claims)
            .HasForeignKey(entity => entity.MarketplaceListingId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Student)
            .WithMany(student => student.MarketplaceClaims)
            .HasForeignKey(entity => entity.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.ApprovedBy)
            .WithMany()
            .HasForeignKey(entity => entity.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.RejectedBy)
            .WithMany()
            .HasForeignKey(entity => entity.RejectedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
