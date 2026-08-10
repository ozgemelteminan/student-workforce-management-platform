using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.DeviceName).HasMaxLength(200);
        builder.Property(entity => entity.IpAddress).HasMaxLength(64);
        builder.Property(entity => entity.ExpiresAt).IsRequired();
        builder.HasIndex(entity => entity.UserId);
        builder.HasIndex(entity => entity.ExpiresAt);
        builder.HasOne(entity => entity.User)
            .WithMany(user => user.Sessions)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
