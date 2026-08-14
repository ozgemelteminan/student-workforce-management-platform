using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("TaskComments");
        builder.ConfigureAuditableEntity();
        builder.ConfigureSoftDelete();
        builder.HasQueryFilter(entity => entity.DeletedAt == null && entity.Task!.DeletedAt == null);
        builder.Property(entity => entity.Content).HasMaxLength(8000).IsRequired();
        builder.Property(entity => entity.Visibility).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => entity.TaskId);
        builder.HasIndex(entity => new { entity.TaskId, entity.CreatedAt });
        builder.HasOne(entity => entity.Task)
            .WithMany(task => task.Comments)
            .HasForeignKey(entity => entity.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Author)
            .WithMany()
            .HasForeignKey(entity => entity.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
