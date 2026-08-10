using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.TokenHash).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.ExpiresAt).IsRequired();
        builder.HasIndex(entity => entity.SessionId);
        builder.HasIndex(entity => entity.TokenHash).IsUnique();
        builder.HasIndex(entity => entity.ExpiresAt);
        builder.HasOne(entity => entity.Session)
            .WithMany(session => session.RefreshTokens)
            .HasForeignKey(entity => entity.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
