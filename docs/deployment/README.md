# Deployment

This repository is container-ready, but production must not run from development defaults.

## Required Production Configuration

Set `ASPNETCORE_ENVIRONMENT=Production` and provide explicit values for:

- `DATABASE_CONNECTION_STRING`
- `REDIS_CONNECTION_STRING`
- `AllowedHosts`
- `Cors__AllowedOrigins__0` and any additional trusted frontend origins
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SigningKey` with a secure 32+ character secret from the deployment secret store
- `Email__Provider` as `SMTP` or `SENDGRID`, with the provider credentials configured through secrets
- `Storage__Provider=S3`
- `Storage__S3__BucketName`
- `Storage__S3__AccessKey`
- `Storage__S3__SecretKey`
- `DataProtection__KeysPath` pointing to durable protected key storage

Development-only email, local file storage, wildcard hosts, missing CORS origins, placeholder JWT keys, and implicit local database fallback are rejected in Production.

## Startup

For local development, use:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```

For production-like deployment, build the images with `docker-compose.yml` plus `docker-compose.prod.yml` and inject the required environment variables through the hosting platform.

Apply migrations before serving traffic:

```bash
dotnet ef database update --project backend/src/StudentWorkforceManagement.Infrastructure --startup-project backend/src/StudentWorkforceManagement.Api
```

## Verification

Before release, run:

```bash
dotnet build backend/StudentWorkforceManagement.sln --no-restore -m:1
dotnet test backend/StudentWorkforceManagement.sln --no-build
cd frontend
npm run typecheck
npm run lint
npm run test
npm run build
npm audit
```

The API exposes readiness at `/health/ready` and liveness at `/health/live`.
