using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class MeetingParticipantConfiguration : IEntityTypeConfiguration<MeetingParticipant>
{
    public void Configure(EntityTypeBuilder<MeetingParticipant> builder)
    {
        builder.ToTable("MeetingParticipants");
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.CampusPresence).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.AvailableRangesJson).HasMaxLength(12000);
        builder.Property(entity => entity.Note).HasMaxLength(1000);
        builder.HasIndex(entity => new { entity.MeetingId, entity.StudentId }).IsUnique();
        builder.HasOne(entity => entity.Meeting).WithMany(meeting => meeting.Participants).HasForeignKey(entity => entity.MeetingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.Student).WithMany(student => student.MeetingParticipants).HasForeignKey(entity => entity.StudentId).OnDelete(DeleteBehavior.Restrict);
    }
}
