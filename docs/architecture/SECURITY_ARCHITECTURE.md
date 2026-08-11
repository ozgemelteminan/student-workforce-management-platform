# Security Architecture

Passwords are stored only in `User.PasswordHash` and are produced by ASP.NET Core Identity password hashing. Security tokens for invitations, resets, and refresh rotation remain hash-only in persistence.

JWT signing configuration comes from environment-backed configuration. Repository values are placeholders only and must be replaced outside source control for production.

Raw passwords, JWT signing keys, refresh tokens, invitation tokens, reset tokens, storage credentials, and email provider credentials must not be logged. Application logging currently records request names, not payloads.

Local storage rejects traversal, rooted paths, and backslash-separated paths. Local download URLs are application-relative contracts; they do not expose `file://` or host filesystem paths.

HTTP authentication uses JWT bearer tokens. Token validation requires a matching active session row and rejects revoked, expired, or user-mismatched sessions. SignalR reads `access_token` from the query string only for `/hubs/notifications`.

Security headers are applied by API middleware: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and `X-Permitted-Cross-Domain-Policies`. CORS is configured by explicit `Cors:AllowedOrigins`; wildcard origins are not used with credentials.

Hangfire Dashboard is available at `/admin/hangfire` and requires an authenticated `ADMIN` role.
