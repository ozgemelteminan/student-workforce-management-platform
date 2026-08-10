# Student Workforce Management Platform

Production-oriented full-stack scaffold for a university department student workforce management system.

This repository currently contains the foundation architecture only. Product workflows are intentionally not implemented in this phase.

## Architecture

- Backend: ASP.NET Core Web API, Clean Architecture, .NET 9
- Frontend: React, TypeScript, Vite, Tailwind CSS
- Database: PostgreSQL
- Cache/job support: Redis-compatible foundation
- Storage: object-storage-ready architecture for direct/multipart 1 GB-safe uploads

## Canonical Foundation Decisions

- Student number is not part of the system model.
- `TaskAssignmentHistory` is the canonical assignment history entity.
- `TaskRequest` is the canonical persistence model for extension and reassignment requests.
- `SubmissionVersion` is the canonical submission history model.
- REVIEWER approves/rejects submissions; TASK_MANAGER does not by default.
- Current API version foundation is `/api/v1`.
- Persisted timestamps must use UTC.

## Local Development

Backend solution:

```bash
cd backend
dotnet restore
dotnet build
```

Frontend:

```bash
cd frontend
npm install
npm run typecheck
npm run test
```

Docker foundation:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

## Documentation

See `docs/` for architecture, API, permissions, development, deployment, and compliance placeholders.
