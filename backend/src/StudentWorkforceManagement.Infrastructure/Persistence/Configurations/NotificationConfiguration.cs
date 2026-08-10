using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.Type).HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Message).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.RelatedEntityType).HasMaxLength(120);
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(256);
        builder.HasIndex(entity => entity.UserId);
        builder.HasIndex(entity => entity.IsRead);
        builder.HasIndex(entity => entity.CreatedAt);
        builder.HasIndex(entity => new { entity.UserId, entity.IsRead, entity.CreatedAt });
        builder.HasIndex(entity => entity.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
        builder.HasOne(entity => entity.User)
            .WithMany(user => user.Notifications)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
