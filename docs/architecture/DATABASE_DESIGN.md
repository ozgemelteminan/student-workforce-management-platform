# Database Design

This document captures the initial domain and persistence baseline for the Student Workforce Management Platform. It describes schema intent only; application workflows, CQRS handlers, API endpoints, and frontend screens are intentionally outside this phase.

## Persistence Baseline

- `ApplicationDbContext` lives in `StudentWorkforceManagement.Infrastructure/Persistence` and applies entity configurations from the Infrastructure assembly.
- PostgreSQL is the production relational provider through `Npgsql.EntityFrameworkCore.PostgreSQL`.
- The initial migration is `20260810203329_InitialDomainSchema`.
- Domain entities remain persistence-ignorant and do not reference Entity Framework, ASP.NET Core, API, or Infrastructure assemblies.

## Core Domain Areas

- Identity and access foundation: `User`, `Role`, `Invitation`, `Session`, `RefreshToken`.
- Student profile and capability planning: `Student`, `Skill`, `StudentSkill`, `CourseSchedule`, `Availability`, `Semester`.
- Task lifecycle: `Task`, `TaskAssignmentHistory`, `TaskRequiredSkill`, `TaskDependency`, `TaskChecklistItem`, `TaskComment`, `TaskSubmission`, `SubmissionVersion`, `TaskRequest`, `TaskReview`.
- Marketplace workflow: `MarketplaceListing`, `MarketplaceClaim`.
- Department content and communication: `FileFolder`, `DepartmentFile`, `Announcement`, `Notification`, `NotificationPreference`, `EmailDelivery`.
- Operational support: `AuditLog`, `Feedback`, `TaskTemplate`, `RecurringTask`, `SystemSetting`.

## Schema Rules

- Business enums are stored as readable strings, not integer ordinals.
- Files are represented by owned `FileMetadata` values with storage keys and metadata only; the database does not store binary file payloads.
- The student model intentionally does not include `StudentNumber`.
- Soft-deletable entities expose `DeletedAt` and use query filters where the aggregate is user-facing or file-facing.
- Optimistic concurrency uses GUID `ConcurrencyToken` columns on task, assignment, request, submission, marketplace, availability, semester, user, recurring task, and settings entities.

## Key Constraints And Indexes

- Unique identity and lookup values: user email, role name, student email, skill name, category name, semester name, system setting key.
- Assignment safety: at most one active assignment history row per task through a filtered unique index.
- Request safety: at most one pending request per task and request type through a filtered unique index.
- Submission versioning: submission versions are unique per submission and version number.
- Marketplace claims: duplicate student claims are blocked, and only one pending or approved claim can exist per listing.
- File metadata: storage keys are unique for department files and submission versions.
- Range checks protect schedule/availability time windows, task/template estimated duration, feedback rating, dependency self-reference, submission version number, file size, checklist order, and email attempt count.

## Phase Boundaries

The current schema is intentionally behavior-light. It does not yet implement CQRS handlers, auth endpoints, database seeding beyond future migrations, background reminders, business workflow services, or API controllers. Those belong to later implementation phases.
