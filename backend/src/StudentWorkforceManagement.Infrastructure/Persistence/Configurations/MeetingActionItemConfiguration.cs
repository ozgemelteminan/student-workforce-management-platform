using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class MeetingActionItemConfiguration : IEntityTypeConfiguration<MeetingActionItem>
{
    public void Configure(EntityTypeBuilder<MeetingActionItem> builder)
    {
        builder.ToTable("MeetingActionItems");
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        builder.HasOne(entity => entity.Meeting).WithMany(meeting => meeting.ActionItems).HasForeignKey(entity => entity.MeetingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.AssignedStudent).WithMany(student => student.MeetingActionItems).HasForeignKey(entity => entity.AssignedStudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Task).WithMany(task => task.MeetingActionItems).HasForeignKey(entity => entity.TaskId).OnDelete(DeleteBehavior.Restrict);
    }
}
