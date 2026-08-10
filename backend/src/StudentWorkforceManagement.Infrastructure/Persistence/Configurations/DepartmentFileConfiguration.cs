using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class DepartmentFileConfiguration : IEntityTypeConfiguration<DepartmentFile>
{
    public void Configure(EntityTypeBuilder<DepartmentFile> builder)
    {
        builder.ToTable("DepartmentFiles", table =>
        {
            table.HasCheckConstraint("CK_DepartmentFiles_FileSize", "\"FileSize\" >= 0");
        });
        builder.ConfigureAuditableEntity();
        builder.ConfigureSoftDelete();
        builder.HasQueryFilter(entity => entity.DeletedAt == null);
        builder.Property(entity => entity.FileStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.OwnsOne(entity => entity.File, owned => owned.ConfigureFileMetadata());
        builder.HasIndex(entity => entity.FolderId);
        builder.HasIndex(entity => entity.UploadedById);
        builder.HasOne(entity => entity.Folder)
            .WithMany(folder => folder.Files)
            .HasForeignKey(entity => entity.FolderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.UploadedBy)
            .WithMany()
            .HasForeignKey(entity => entity.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
