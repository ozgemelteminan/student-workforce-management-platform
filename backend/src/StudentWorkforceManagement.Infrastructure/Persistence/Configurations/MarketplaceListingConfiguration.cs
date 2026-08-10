using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class MarketplaceListingConfiguration : IEntityTypeConfiguration<MarketplaceListing>
{
    public void Configure(EntityTypeBuilder<MarketplaceListing> builder)
    {
        builder.ToTable("MarketplaceListings");
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ApprovalMode).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => entity.TaskId);
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.ExpiresAt);
        builder.HasOne(entity => entity.Task)
            .WithMany(task => task.MarketplaceListings)
            .HasForeignKey(entity => entity.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.PublishedBy)
            .WithMany()
            .HasForeignKey(entity => entity.PublishedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
