# Student Workforce Management Frontend

React, TypeScript, Vite, Tailwind CSS, React Router, and TanStack Query provide the frontend foundation.

## Commands

```bash
npm install
npm run dev
npm run typecheck
npm run test
npm run lint
npm run build
```

The Vite dev server uses port `5173`, which matches the backend development CORS configuration.

## Environment

Copy `.env.example` for local development and set:

```bash
VITE_API_BASE_URL=http://localhost:8080
VITE_API_VERSION=/api/v1
VITE_DISPLAY_TIMEZONE=Europe/Istanbul
```

Frontend environment variables must not contain backend secrets, JWT signing keys, database credentials, SMTP secrets, or storage credentials.

## Architecture

Phase 1 foundation code is organized around:

- `src/app/providers`: application provider composition
- `src/app/routes`: router foundation
- `src/features/auth`: route protection foundation
- `src/lib/api`: canonical API transport and ProblemDetails handling
- `src/lib/auth`: auth/session state and token storage
- `src/lib/query`: TanStack Query defaults and key conventions
- `src/lib/date-time`: Europe/Istanbul timezone utilities
- `src/lib/utils`: shared utility primitives such as `cn(...)`

Product workflows and final application screens are intentionally deferred to later frontend phases.

## Auth Token Storage

The backend currently returns client-managed access and refresh tokens from login. The frontend stores the active session in `sessionStorage`, not `localStorage`, so tokens are scoped to the current browser tab/session and handled only through the centralized auth layer.

The refresh endpoint rotates the refresh token but does not currently return a new access token. The API client still coordinates one refresh attempt for concurrent `401` responses and fails closed when no new access token can be obtained.

## Timezone

All user-facing application timestamp helpers render with `Europe/Istanbul` through `date-fns` and `date-fns-tz`. Components should use `src/lib/date-time` rather than raw `toLocaleString()` calls.

## Signed URLs

Signed download URLs are temporary credentials. Request them on demand, do not log them, do not persist them, and do not store them as long-lived entity state.

