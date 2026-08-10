using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.ConfigureAuditableEntity();
        builder.ConfigureSoftDelete();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.Email).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.PasswordHash).HasMaxLength(1024);
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.HasIndex(entity => entity.Email).IsUnique();
        builder.HasOne(entity => entity.Role)
            .WithMany(role => role.Users)
            .HasForeignKey(entity => entity.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Student)
            .WithOne(student => student.User)
            .HasForeignKey<Student>(student => student.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
