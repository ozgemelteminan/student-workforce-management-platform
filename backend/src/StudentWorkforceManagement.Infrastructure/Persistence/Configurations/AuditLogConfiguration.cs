using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.Action).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.EntityType).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.OldValue).HasColumnType("jsonb");
        builder.Property(entity => entity.NewValue).HasColumnType("jsonb");
        builder.Property(entity => entity.IpAddress).HasMaxLength(64);
        builder.Property(entity => entity.CorrelationId).HasMaxLength(128);
        builder.HasIndex(entity => entity.UserId);
        builder.HasIndex(entity => entity.EntityId);
        builder.HasIndex(entity => entity.CreatedAt);
        builder.HasIndex(entity => entity.Action);
        builder.HasOne(entity => entity.User)
            .WithMany(user => user.AuditLogs)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
