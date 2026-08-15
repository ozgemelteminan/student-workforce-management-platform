# Reference Data

Categories and Skills are separate reference-data concepts.

Categories describe the type of task, such as `Administrative` or `Technical / IT`. Task creation and task reporting use Categories.

Skills describe capabilities required by tasks or possessed by students, such as `Microsoft Excel`, `Programming`, or `Content Writing`. Task required skills and student skill profiles use Skills.

## Initial Production Seed

Run the production seed once after migrations are applied:

```bash
ASPNETCORE_ENVIRONMENT=Production DOTNET_ENVIRONMENT=Production DATABASE_CONNECTION_STRING='<production PostgreSQL connection string>' dotnet run --no-launch-profile --project backend/src/StudentWorkforceManagement.Api/StudentWorkforceManagement.Api.csproj -- --seed-production-reference-data
```

The seed command uses only PostgreSQL/EF Core services. It does not start the web server and does not require CORS, email, Redis, Hangfire, SignalR, or storage configuration.

Seeded Categories:

- Administrative
- Academic Support
- Technical / IT
- Content & Communication
- Event Support
- Data & Reporting

Seeded Skills:

- Microsoft Excel
- Microsoft Word
- Microsoft PowerPoint
- Canva
- Data Entry
- Data Analysis
- Research
- Documentation
- Content Writing
- Communication
- Social Media
- Event Support
- Event Coordination
- Technical Support
- Programming
- Web Development
- Git / GitHub
- Graphic Design

## Administration

ADMIN users manage Categories and Skills in `Settings -> Categories & Skills`.

Reference records can be deactivated and reactivated. Inactive records are hidden from new task and student-skill selections, while existing historical relationships remain readable.

API overview:

- `GET /api/v1/categories`
- `GET /api/v1/categories?includeInactive=true`
- `GET /api/v1/categories/{id}`
- `POST /api/v1/categories`
- `PUT /api/v1/categories/{id}`
- `POST /api/v1/categories/{id}/deactivate`
- `POST /api/v1/categories/{id}/reactivate`
- `GET /api/v1/skills`
- `GET /api/v1/skills?includeInactive=true`
- `GET /api/v1/skills/{id}`
- `POST /api/v1/skills`
- `PUT /api/v1/skills/{id}`
- `POST /api/v1/skills/{id}/deactivate`
- `POST /api/v1/skills/{id}/reactivate`
