using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.PreferenceType).HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Channel).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => new { entity.UserId, entity.PreferenceType, entity.Channel }).IsUnique();
        builder.HasOne(entity => entity.User)
            .WithMany(user => user.NotificationPreferences)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
