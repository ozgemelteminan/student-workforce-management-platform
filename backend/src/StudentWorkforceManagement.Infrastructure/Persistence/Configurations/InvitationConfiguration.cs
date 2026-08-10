using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.Email).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.TokenHash).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.ExpiresAt).IsRequired();
        builder.HasIndex(entity => entity.Email);
        builder.HasIndex(entity => entity.TokenHash).IsUnique();
        builder.HasIndex(entity => entity.ExpiresAt);
        builder.HasOne(entity => entity.CreatedBy)
            .WithMany(user => user.InvitationsCreated)
            .HasForeignKey(entity => entity.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
