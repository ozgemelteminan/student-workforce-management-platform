# Deployment

Required production configuration:

- `DATABASE_CONNECTION_STRING`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SigningKey`
- `Email:Provider` plus provider credentials
- `Storage:Provider` plus S3-compatible bucket/credential settings for production object storage
- `BackgroundJobs:HangfireSchemaName=hangfire`
- `REDIS_CONNECTION_STRING` when Redis/SignalR backplane is enabled

Hangfire uses PostgreSQL persistent storage and initializes its own tables in the `hangfire` schema. Do not create EF entities or migrations for Hangfire internal tables.

Local storage and the Development email provider are development-safe adapters and must not be silent production fallbacks.
