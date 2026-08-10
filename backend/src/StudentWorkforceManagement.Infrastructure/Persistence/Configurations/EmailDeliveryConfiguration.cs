using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class EmailDeliveryConfiguration : IEntityTypeConfiguration<EmailDelivery>
{
    public void Configure(EntityTypeBuilder<EmailDelivery> builder)
    {
        builder.ToTable("EmailDeliveries", table =>
        {
            table.HasCheckConstraint("CK_EmailDeliveries_Attempts", "\"Attempts\" >= 0");
        });
        builder.ConfigureAuditableEntity();
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.RecipientEmail).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Subject).HasMaxLength(240).IsRequired();
        builder.Property(entity => entity.TemplateKey).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.TemplateDataJson).HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ProviderName).HasMaxLength(120);
        builder.Property(entity => entity.ProviderMessageId).HasMaxLength(256);
        builder.Property(entity => entity.FailureReason).HasMaxLength(4000);
        builder.HasIndex(entity => entity.IdempotencyKey).IsUnique();
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.NextRetryAt);
    }
}
