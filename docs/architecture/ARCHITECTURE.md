# Infrastructure Architecture

Infrastructure implements mechanisms behind Application abstractions. Application and Domain do not reference provider SDKs.

## Providers

- Password hashing: ASP.NET Core Identity `PasswordHasher<User>` behind `IPasswordService`.
- Secure random tokens: `RandomNumberGenerator` behind `ISecureTokenGenerator`.
- Access tokens: HMAC-SHA256 JWT construction behind `IAccessTokenService`; issuer, audience, key, and lifetime come from `Jwt` options.
- Email: `IEmailService` persists `EmailDelivery` intent; `EmailDispatchJob` calls the configured `IEmailProvider`.
- Email secret protection: one-time invitation/reset template values use a sensitive template-data channel. Infrastructure protects them with ASP.NET Core Data Protection before persistence and clears protected values after successful dispatch.
- Storage: `IFileStorage` selects `Local` or `S3` through configuration.
- Real-time notifications: persistent `Notification` rows remain canonical; SignalR is transport only.
- Background jobs: Hangfire with PostgreSQL storage in the dedicated `hangfire` schema.
- Redis: currently used for the optional SignalR backplane and Redis health verification. A general cache abstraction is deferred until stable-data query use cases consume it.

## Data Protection

`DataProtection:KeysPath` configures a persistent key-ring location. Docker Compose mounts a named volume at `/app/storage/keys` so queued email secrets survive container restarts. Production deployments must mount durable key storage and protect key material at rest using the hosting platform's secret/key-management controls.

## Retry Ownership

Hangfire owns job-level retries. HTTP providers use bounded `Microsoft.Extensions.Http.Resilience` retries. SDK retries should remain disabled or bounded by provider configuration to avoid retry multiplication.
