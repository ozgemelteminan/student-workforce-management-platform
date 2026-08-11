using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class ExportRequestConfiguration : IEntityTypeConfiguration<ExportRequest>
{
    public void Configure(EntityTypeBuilder<ExportRequest> builder)
    {
        builder.ToTable("ExportRequests", table =>
        {
            table.HasCheckConstraint("CK_ExportRequests_ArtifactFileSize", "\"ArtifactFileSize\" IS NULL OR \"ArtifactFileSize\" >= 0");
        });

        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();

        builder.Property(entity => entity.ExportType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Format).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.FailureReason).HasMaxLength(2000);
        builder.Property(entity => entity.ArtifactStorageKey).HasMaxLength(1024);
        builder.Property(entity => entity.ArtifactFileName).HasMaxLength(255);
        builder.Property(entity => entity.ArtifactMimeType).HasMaxLength(150);
        builder.Property(entity => entity.ArtifactContentHash).HasMaxLength(128);

        builder.HasIndex(entity => entity.RequestingUserId);
        builder.HasIndex(entity => entity.AuthorizedUserId);
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.RequestedAt);
        builder.HasIndex(entity => entity.ExpiresAt);
        builder.HasIndex(entity => entity.IdempotencyKey).IsUnique();

        builder.HasOne(entity => entity.RequestingUser)
            .WithMany()
            .HasForeignKey(entity => entity.RequestingUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
