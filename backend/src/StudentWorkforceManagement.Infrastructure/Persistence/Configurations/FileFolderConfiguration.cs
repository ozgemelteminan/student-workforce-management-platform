using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class FileFolderConfiguration : IEntityTypeConfiguration<FileFolder>
{
    public void Configure(EntityTypeBuilder<FileFolder> builder)
    {
        builder.ToTable("FileFolders");
        builder.ConfigureAuditableEntity();
        builder.ConfigureSoftDelete();
        builder.HasQueryFilter(entity => entity.DeletedAt == null);
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(entity => new { entity.ParentFolderId, entity.Name }).IsUnique();
        builder.HasOne(entity => entity.ParentFolder)
            .WithMany(folder => folder.ChildFolders)
            .HasForeignKey(entity => entity.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
