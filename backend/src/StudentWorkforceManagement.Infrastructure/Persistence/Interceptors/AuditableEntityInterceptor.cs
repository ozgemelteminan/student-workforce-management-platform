using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Common;

namespace StudentWorkforceManagement.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(IUtcClock clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateTimestamps(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow.ToUniversalTime();

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                SetCreated(entry, now);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    private static void SetCreated(EntityEntry<AuditableEntity> entry, DateTimeOffset now)
    {
        entry.Entity.CreatedAt = now;
        entry.Entity.UpdatedAt = now;
    }
}
