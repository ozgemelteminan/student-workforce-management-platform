using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class SubmissionVersionConfiguration : IEntityTypeConfiguration<SubmissionVersion>
{
    public void Configure(EntityTypeBuilder<SubmissionVersion> builder)
    {
        builder.ToTable("SubmissionVersions", table =>
        {
            table.HasCheckConstraint("CK_SubmissionVersions_VersionNumber", "\"VersionNumber\" > 0");
            table.HasCheckConstraint("CK_SubmissionVersions_FileSize", "\"FileSize\" >= 0");
        });
        builder.ConfigureAuditableEntity();
        builder.ConfigureSoftDelete();
        builder.HasQueryFilter(entity =>
            entity.DeletedAt == null &&
            entity.TaskSubmission!.DeletedAt == null &&
            entity.TaskSubmission.Task!.DeletedAt == null &&
            entity.TaskSubmission.SubmittedBy!.DeletedAt == null &&
            entity.UploadedBy!.DeletedAt == null);
        builder.Property(entity => entity.FileStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.OwnsOne(entity => entity.File, owned => owned.ConfigureFileMetadata());
        builder.HasIndex(entity => entity.TaskSubmissionId);
        builder.HasIndex(entity => new { entity.TaskSubmissionId, entity.VersionNumber }).IsUnique();
        builder.HasIndex(entity => entity.UploadedById);
        builder.HasOne(entity => entity.TaskSubmission)
            .WithMany(submission => submission.Versions)
            .HasForeignKey(entity => entity.TaskSubmissionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.UploadedBy)
            .WithMany()
            .HasForeignKey(entity => entity.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
