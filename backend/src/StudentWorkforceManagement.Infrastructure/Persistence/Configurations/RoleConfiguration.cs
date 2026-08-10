using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.Name).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.HasIndex(entity => entity.Name).IsUnique();
    }
}
