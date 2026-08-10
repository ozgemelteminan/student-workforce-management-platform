using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.ConfigureAuditableEntity();
        builder.ConfigureSoftDelete();
        builder.ConfigureConcurrencyToken();
        builder.HasQueryFilter(entity => entity.DeletedAt == null);
        builder.Property(entity => entity.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.LastName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Email).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Department).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.HasIndex(entity => entity.Email).IsUnique();
        builder.HasIndex(entity => entity.IsActive);
    }
}
