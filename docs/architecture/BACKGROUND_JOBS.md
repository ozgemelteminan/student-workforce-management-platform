# Background Jobs

Hangfire is the selected job framework. Production storage is PostgreSQL via Hangfire.PostgreSql with schema isolation in `hangfire`.

Jobs are bounded and retry-safe:

- `DeadlineReminderJob`: creates idempotent reminder notifications for 24h/3h windows.
- `OverdueTaskJob`: marks due tasks overdue and creates a single overdue notification.
- `RecurringTaskJob`: claims a unique `RecurringTaskOccurrence`, uses the Application recurring generation service to create the task, and advances `NextRunAt` through `IRecurringScheduleCalculator`.
- `EmailDispatchJob`: claims queued/retryable `EmailDelivery` rows in batches, decrypts protected one-time template secrets only immediately before sending, clears protected secrets after successful send, and calls the active provider.
- `OrphanFileCleanupJob`: marks stale pending uploads failed after a grace period.
- `MarketplaceClaimExpirationJob`: expires pending claims whose expiration has passed.
- `SemesterRolloverJob`: archives active semesters after their end date.
- `RetentionCleanupJob`: no destructive automation until entity-specific retention/deletion/anonymization policy identifies eligible records.
- `DataExportJob`: claims queued durable export requests, generates CSV/XLSX/PDF artifacts, persists completion/failure state, and stores authorized expiring download metadata.

Recurring task duplicate protection is database-backed through a unique index on `(RecurringTaskId, ScheduledRunAt)`.

Retry multiplication target: Hangfire 3 attempts x HTTP resilience bounded attempts x provider SDK bounded/disabled retries. Keep total provider calls intentional and documented per provider.
