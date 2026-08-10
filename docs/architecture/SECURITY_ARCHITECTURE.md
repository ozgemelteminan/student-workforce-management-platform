# Security Architecture

Passwords are stored only in `User.PasswordHash` and are produced by ASP.NET Core Identity password hashing. Security tokens for invitations, resets, and refresh rotation remain hash-only in persistence.

JWT signing configuration comes from environment-backed configuration. Repository values are placeholders only and must be replaced outside source control for production.

Raw passwords, JWT signing keys, refresh tokens, invitation tokens, reset tokens, storage credentials, and email provider credentials must not be logged. Application logging currently records request names, not payloads.

Local storage rejects traversal, rooted paths, and backslash-separated paths. Local download URLs are application-relative contracts; they do not expose `file://` or host filesystem paths.
