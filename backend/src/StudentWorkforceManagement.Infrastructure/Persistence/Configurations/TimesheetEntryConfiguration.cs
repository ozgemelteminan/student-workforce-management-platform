using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Configurations;

public sealed class TimesheetEntryConfiguration : IEntityTypeConfiguration<TimesheetEntry>
{
    public void Configure(EntityTypeBuilder<TimesheetEntry> builder)
    {
        builder.ToTable("TimesheetEntries", table =>
        {
            table.HasCheckConstraint("CK_TimesheetEntries_Minutes", "\"Minutes\" > 0");
        });
        builder.ConfigureAuditableEntity();
        builder.ConfigureConcurrencyToken();
        builder.Property(entity => entity.Note).HasMaxLength(1000);
        builder.HasIndex(entity => entity.TimesheetWeekId);
        builder.HasIndex(entity => entity.TaskId);
        builder.HasOne(entity => entity.TimesheetWeek).WithMany(week => week.Entries).HasForeignKey(entity => entity.TimesheetWeekId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.Task).WithMany(task => task.TimesheetEntries).HasForeignKey(entity => entity.TaskId).OnDelete(DeleteBehavior.Restrict);
    }
}
