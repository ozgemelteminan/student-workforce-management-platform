using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    public static void ConfigureAuditableEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.UpdatedAt).IsRequired();
        builder.HasIndex(entity => entity.CreatedAt);
    }

    public static void ConfigureSoftDelete<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ISoftDeletable
    {
        builder.Property(entity => entity.DeletedAt);
        builder.HasIndex(entity => entity.DeletedAt);
    }

    public static void ConfigureConcurrencyToken<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IHasConcurrencyToken
    {
        builder.Property(entity => entity.ConcurrencyToken)
            .IsRequired()
            .IsConcurrencyToken();
    }

    public static void ConfigureFileMetadata<TOwner>(this OwnedNavigationBuilder<TOwner, FileMetadata> builder)
        where TOwner : class
    {
        builder.Property(file => file.FileName).HasColumnName("FileName").HasMaxLength(255).IsRequired();
        builder.Property(file => file.StorageKey).HasColumnName("StorageKey").HasMaxLength(1024).IsRequired();
        builder.Property(file => file.FileSize).HasColumnName("FileSize").IsRequired();
        builder.Property(file => file.MimeType).HasColumnName("MimeType").HasMaxLength(150).IsRequired();
        builder.Property(file => file.FileExtension).HasColumnName("FileExtension").HasMaxLength(20).IsRequired();
        builder.Property(file => file.ContentHash).HasColumnName("ContentHash").HasMaxLength(128);
        builder.HasIndex(file => file.StorageKey).IsUnique();
    }
}
