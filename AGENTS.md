# AGENTS.md
# Student Workforce Management Platform
# Repository Architecture & Implementation Enforcement Rules

## 0. PURPOSE

You are the coding agent responsible for implementing and maintaining the **Student Workforce Management Platform**.

This repository is a production-oriented full-stack application for managing university department student workers, their tasks, assignments, schedules, availability, submissions, requests, notifications, files, workload, announcements, analytics, and administrative operations.

You MUST treat:

- `MASTER_SPECIFICATION.md`
- this `AGENTS.md`
- the repository architecture
- existing source code
- existing database schema and migrations
- existing tests

as authoritative sources.

The agent MUST NOT invent business rules, entities, endpoints, permissions, workflows, technologies, or architectural patterns that contradict these sources.

When a requirement is ambiguous, the agent MUST NOT silently invent a solution.

---

# 1. SOURCE OF TRUTH HIERARCHY

When multiple sources provide information, use the following priority:

1. `MASTER_SPECIFICATION.md`
2. `AGENTS.md`
3. Existing implemented architecture
4. Existing tests
5. Existing documentation
6. General engineering best practices

If two requirements conflict:

- identify the conflict,
- do not silently choose one,
- explain the conflict,
- ask for clarification when the conflict materially affects implementation.

Do NOT overwrite an explicit specification with personal assumptions.

---

## Frontend Agent Rules

Before any frontend task, read the `Frontend Product, UX, and Design Specification` section of `MASTER_SPECIFICATION.md`.

For frontend/API integration, the runtime OpenAPI contract is authoritative.

Never invent backend endpoints, request fields, response fields, enum values, permissions, persistence behavior, or workflow states.

If a required UX workflow cannot be implemented with the current API, report the exact API/contract gap instead of:

- faking success
- using permanent mock data
- creating frontend-only persistence
- silently changing the workflow

Do not modify backend architecture merely for frontend convenience. Any genuine backend gap must be identified explicitly and fixed only through the smallest coherent change when the task actually requires it.

Use workflow-first frontend composition. Do not create one page/button/component per backend endpoint. Related API capabilities should be composed into coherent product workflows.

Reuse the canonical frontend design system defined in `MASTER_SPECIFICATION.md`. Do not independently invent:

- new brand colors
- new destructive colors
- new button styles
- new card styles
- new status semantics
- new spacing systems
- new modal/drawer patterns

Preserve the canonical visual system:

- warm off-white workspace
- charcoal navigation
- white surfaces
- restrained brand red
- separate destructive red semantics

Brand red and destructive red must remain semantically separate.

Do not leave permanent production-looking mock data in completed frontend workflows.

A frontend workflow is not complete until it has:

- real API integration
- loading state
- empty state
- error state
- authorization-aware UI behavior where relevant

Frontend role-based visibility is a UX convenience only. Backend authorization remains authoritative. Handle `401` and `403` correctly.

Use the canonical API client layer. Do not scatter independent raw `fetch()` calls or duplicate token-refresh logic across components.

Use TanStack Query as the primary server-state mechanism. Do not unnecessarily mirror backend state into global client stores.

Signed download URLs are temporary credentials:

- request them on demand
- never persist or log them
- never store them as long-lived entity state
- do not unnecessarily buffer large downloads in JavaScript memory

All user-facing application timestamps must use the centralized timezone utilities and render according to `Europe/Istanbul`, independent of browser timezone.

Do not scatter raw date/time formatting logic across components.

Respect the canonical large-file policy:

- up to 1 GB per file
- no Base64 file transport
- no full-file buffering
- use direct signed upload/download flows where applicable

Preserve accessibility requirements:

- visible keyboard focus
- semantic controls
- explanatory validation messages
- no color-only meaning
- accessible dialogs, menus, tables, and command palette

Implement frontend work in controlled phases according to the implementation order in `MASTER_SPECIFICATION.md`.

Do not attempt to build the entire frontend in one uncontrolled pass.

After each frontend phase:

- keep the app runnable
- run the relevant frontend build/type-check/tests
- report any API contract gaps
- do not begin the next phase unless explicitly requested

Do not silently weaken tests, authorization, validation, or API contracts to make frontend code pass.

Prefer production-ready reusable primitives and domain components over large page components with duplicated logic.

Treat `MASTER_SPECIFICATION.md` and `AGENTS.md` as binding project instructions. When handwritten legacy API examples conflict with runtime OpenAPI, runtime OpenAPI wins for frontend integration and the conflict must be reported.

---

# 2. CORE PROJECT PRINCIPLES

The application MUST follow:

- Clean Architecture
- Domain-driven organization
- SOLID principles
- separation of concerns
- dependency inversion
- explicit authorization
- secure-by-default behavior
- testability
- maintainability
- production-grade error handling
- production-grade logging
- observability
- concurrency protection
- data privacy
- auditability

The project MUST NOT degrade into a simple CRUD/Todo application.

Business workflows must remain explicit.

---

# 3. NO HALLUCINATED REQUIREMENTS

The agent MUST NOT invent:

- new business rules
- new roles
- new permissions
- new database entities
- new API endpoints
- new task states
- new request states
- new notification behaviors
- new file policies
- new approval workflows
- new authentication rules
- new external integrations

unless they are:

1. explicitly required by the specification,
2. required to implement an already-defined feature technically, or
3. explicitly approved by the user.

If an implementation detail is technically necessary but unspecified, choose the smallest conventional implementation and document the assumption.

---

# 4. FEATURE COMPLETENESS RULE

A feature is NOT considered implemented merely because its primary code exists.

Every feature MUST be evaluated across all relevant layers:

- Domain
- Application
- Infrastructure
- API
- Database
- Authorization
- Validation
- Frontend
- API client
- UI
- Notifications
- Logging/Audit
- Tests
- Documentation
- Dependency Injection
- Configuration

When adding a service, repository, handler, validator, provider, or other dependency, the agent MUST ensure it is explicitly registered in the dependency injection container.

No implementation is complete until its runtime dependency graph is valid.

---

# 5. EXISTING CODE PROTECTION

Before modifying an existing file:

1. inspect the current implementation,
2. understand its dependencies,
3. preserve unrelated functionality,
4. make the smallest safe change.

The agent MUST NOT rewrite unrelated code merely for stylistic reasons.

The agent MUST NOT remove existing functionality unless the specification explicitly requires it.

The agent MUST NOT replace working architecture with a different architecture without explicit approval.

---

# 6. NO CODE TRUNCATION

The agent MUST NEVER truncate source files during modifications.

The agent MUST NOT leave placeholders such as:

```text
// ... existing code ...
// ... rest of implementation ...
// TODO: existing logic
// omitted for brevity
```

if doing so would result in an incomplete file.

Every modified file MUST remain syntactically complete and valid.

If the editing mechanism supports patches or diffs, use proper patch/diff operations.

Never save an intentionally incomplete source file.

---

# 7. ARCHITECTURE

The backend MUST follow:

```text
API
 ↓
Application
 ↓
Domain
```

Infrastructure may depend on Application and Domain.

Domain MUST NOT depend on:

- Entity Framework
- ASP.NET
- HTTP
- controllers
- Infrastructure
- database providers
- external APIs

Application MUST contain business use cases and abstractions.

Infrastructure MUST contain implementation details.

API MUST remain thin and delegate business logic to Application.

---

# 8. PROJECT STRUCTURE

The canonical repository structure is defined by `MASTER_SPECIFICATION.md`.

The agent MUST preserve the repository structure.

Expected major areas include:

```text
Student-Workforce-Management-Platform/
├── AGENTS.md
├── MASTER_SPECIFICATION.md
├── docs/
├── backend/
├── frontend/
├── infrastructure/
└── scripts/
```

Backend:

```text
backend/
├── src/
│   ├── StudentWorkforceManagement.Api/
│   ├── StudentWorkforceManagement.Application/
│   ├── StudentWorkforceManagement.Domain/
│   └── StudentWorkforceManagement.Infrastructure/
└── tests/
```

Frontend:

```text
frontend/
├── src/
└── tests/
```

The agent MUST NOT create random top-level directories for feature implementations.

---

# 9. DOMAIN RULES

Entities belong in:

```text
Domain/Entities/
```

Enums belong in:

```text
Domain/Enums/
```

Value objects belong in:

```text
Domain/ValueObjects/
```

Domain events belong in:

```text
Domain/Events/
```

Domain exceptions belong in:

```text
Domain/Exceptions/
```

The domain model MUST represent business concepts rather than HTTP/database implementation details.

---

# 10. CORE DOMAIN ENTITIES

The implementation MUST account for the entities defined by the specification, including:

```text
User
Student
Role

Invitation
Session
RefreshToken

Skill
StudentSkill
Category
Semester
CourseSchedule
Availability

Task
TaskAssignment
TaskRequiredSkill
TaskDependency
TaskComment
TaskChecklistItem
TaskSubmission
SubmissionFile

ExtensionRequest
ReassignmentRequest

MarketplaceListing
MarketplaceClaim

DepartmentFile
Announcement

Notification
NotificationPreference

Feedback
TaskTemplate
RecurringTask

EmailDelivery
AuditLog
```

The agent MUST NOT silently remove or merge these entities simply because a simpler design appears possible.

---

# 11. TASK ASSIGNMENT HISTORY

`TaskAssignment` MUST preserve assignment history where required by the specification.

Reassignment MUST NOT destroy historical assignment information.

Historical data must retain appropriate information such as:

- assignment time
- unassignment time
- assignee
- assignment reason
- assignment status

Do not model only the current assignment if historical auditing is required.

---

# 12. TASK REQUIRED SKILLS

`TaskRequiredSkill` MUST explicitly represent the relationship between a task and a required skill.

The implementation MUST support the fields required by the specification, such as:

- TaskId
- SkillId
- MinimumLevel

Do not leave this relationship as an undefined placeholder.

---

# 13. STUDENT SKILLS

`StudentSkill` MUST represent the Student ↔ Skill relationship.

Skill proficiency belongs to the student-skill relationship.

The implementation MUST support the defined `SkillLevel` values.

---

# 14. AUTHENTICATION

Authentication is invite-only unless explicitly changed by the specification.

Student registration MUST NOT become unrestricted public registration.

Supported authentication workflows include:

- login
- invitation
- invitation acceptance
- forgot password
- reset password
- session management
- refresh token management
- logout
- revoke sessions

Authentication endpoints MUST be protected against abuse.

---

# 15. PASSWORD SECURITY

Passwords MUST:

- never be stored in plaintext,
- use secure password hashing,
- follow ASP.NET Identity security recommendations,
- support password reset securely.

Password-reset tokens MUST:

- expire,
- be single-use,
- be securely generated,
- never be logged.

---

# 16. SESSION MANAGEMENT

The system MUST support session/device management where specified.

Session-related functionality must support:

- viewing active sessions,
- revoking a session,
- revoking all sessions,
- refresh-token rotation where applicable.

Session persistence MUST have corresponding domain/infrastructure representation.

---

# 17. ROLE AND AUTHORIZATION RULES

Authorization MUST be explicit.

Do not rely solely on frontend visibility.

At minimum, permissions must distinguish appropriate responsibilities between:

- ADMIN
- TASK_MANAGER
- REVIEWER
- STUDENT

The frontend hiding a button does NOT constitute authorization.

Every protected endpoint MUST enforce authorization server-side.

---

# 18. FILE MANAGEMENT

Files may be up to:

**1 GB per file.**

The system MUST NOT load complete files into application memory.

The implementation MUST use:

- direct-to-object-storage uploads,
- signed URLs,
- multipart uploads,
- streaming,
- or another equivalent memory-safe approach.

The API MUST NOT implement a 1 GB upload by buffering the entire file in RAM.

File uploads MUST support validation of:

- file size
- MIME type
- extension
- upload status
- ownership/access permissions

---

# 19. FILE STORAGE

Task files and Department Files are separate concepts.

Task files belong to task/submission workflows.

Department Files provide shared department-level storage such as:

```text
Logos
Templates
Guidelines
Forms
Other department resources
```

Do not merge these storage concepts unless explicitly required.

---

# 20. FILE DOWNLOAD

File upload is not sufficient.

The implementation MUST provide a secure mechanism for:

- downloading files,
- viewing files where supported,
- authorization checks,
- signed/private URLs where appropriate.

Files MUST never become publicly accessible simply because they exist in object storage.

---

# 21. ORPHAN FILE CLEANUP

The system MUST prevent orphaned storage objects.

When database records are deleted, replaced, invalidated, or otherwise detached from files, the corresponding storage objects MUST be handled according to the retention policy.

Background cleanup MUST be available where immediate deletion is unsafe.

---

# 22. DATABASE CONCURRENCY

The system MUST implement optimistic concurrency for mutable entities where concurrent modification is possible.

Examples include:

- Tasks
- Students/Profiles
- Assignments
- Requests
- Marketplace records
- other mutable business entities

Use an appropriate concurrency token such as:

- RowVersion
- timestamp/concurrency token
- equivalent EF Core optimistic concurrency mechanism

The system MUST NOT silently overwrite another user's changes.

---

# 23. 409 CONFLICT

When optimistic concurrency detects a conflicting update, the API MUST return:

```http
409 Conflict
```

The response should communicate that the resource has changed and the client must refresh/retry using the latest state.

Do NOT return `200 OK` for silently overwritten concurrent changes.

---

# 24. MARKETPLACE CONCURRENCY

Marketplace claims are race-condition sensitive.

If two students attempt to claim the same task simultaneously:

- only one valid claim may succeed,
- the database must enforce the necessary consistency,
- the losing request must receive an appropriate conflict response.

Do not rely solely on frontend checks.

---

# 25. MARKETPLACE EXPIRATION

Marketplace claims must have explicit lifecycle handling.

Expired claims/listings MUST transition to the appropriate state automatically.

A background job MUST process marketplace claim expiration.

---

# 26. TIME AND DATE STRATEGY

All persisted timestamps MUST use UTC.

Use:

```csharp
DateTime.UtcNow
```

or an equivalent UTC-safe mechanism.

The agent MUST NOT use server-local time for business logic.

Frontend applications may convert UTC timestamps to the user's local timezone for display.

Database timestamps MUST remain consistent.

---

# 27. SEMESTER ROLLOVER

Semester lifecycle must be explicit.

A background job MUST support semester rollover/archive behavior defined by the specification.

Old schedule and availability information MUST NOT accidentally become active in a new semester.

Historical records must remain auditable.

---

# 28. SOFT DELETE

The system MUST prefer soft deletion/deactivation for records whose history is important.

Examples include:

- Students
- Tasks
- Submissions
- Assignments
- Audit-related entities

Hard deletion MUST only occur where explicitly allowed by the specification, retention policy, or privacy requirements.

Soft-deleted records MUST not appear in normal active queries unless explicitly requested.

---

# 29. KVKK / DATA PRIVACY

The application handles personal data.

The implementation MUST follow the KVKK requirements defined in:

```text
docs/compliance/KVKK.md
docs/compliance/DATA_RETENTION.md
docs/compliance/DATA_DELETION.md
```

Apply:

- data minimization
- purpose limitation
- access control
- retention limits
- secure deletion/anonymization
- auditability
- appropriate protection of personal data

Only information required by the application should be stored.

---

# 30. STUDENT NUMBER

`StudentNumber` MUST NOT be introduced merely because it is commonly used by universities.

If the specification does not require it, do not store it.

The final decision defined by `MASTER_SPECIFICATION.md` is authoritative.

Do not create a second contradictory implementation.

---

# 31. DATA EXPORT

Users must have the ability to request/export their own applicable personal data where required by the specification and KVKK requirements.

Export operations may be asynchronous.

Large exports MUST NOT block normal HTTP requests unnecessarily.

---

# 32. DATA RETENTION

Retention rules MUST be explicit.

Background jobs may perform:

- expired-data cleanup
- anonymization
- deletion where legally permitted
- temporary file cleanup
- expired export cleanup

Never delete audit-critical records merely to simplify storage.

---

# 33. API VERSIONING

All public API endpoints MUST follow the versioning strategy defined by the specification.

The canonical structure is:

```text
/api/v1/...
```

Future breaking changes must use:

```text
/api/v2/...
```

Never write:

```text
/api/v1/v2/...
```

Do not introduce breaking API changes into an existing version.

---

# 34. API COMPLETENESS

Every implemented capability MUST have an appropriate API contract.

The API inventory must remain synchronized with implementation.

Examples include:

```text
/api/v1/auth
/api/v1/users
/api/v1/students
/api/v1/tasks
/api/v1/assignments
/api/v1/requests
/api/v1/submissions
/api/v1/marketplace
/api/v1/skills
/api/v1/categories
/api/v1/semesters
/api/v1/schedules
/api/v1/availability
/api/v1/files
/api/v1/announcements
/api/v1/notifications
/api/v1/feedback
/api/v1/templates
/api/v1/recurring-tasks
/api/v1/analytics
/api/v1/settings
/api/v1/exports
/api/v1/audit
/api/v1/health
```

Do not implement a feature only through frontend mock data.

---

# 35. PAGINATION

Growing collections MUST use pagination.

Paginated endpoints MUST NOT return raw arrays.

The standard response envelope MUST contain fields equivalent to:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 100,
  "totalPages": 5
}
```

The exact canonical contract is defined by `MASTER_SPECIFICATION.md`.

All paginated endpoints MUST use the same response structure.

---

# 36. SEARCH / FILTER / SORT

Task listing and other growing collections MUST support the query parameters defined by the specification.

Examples include:

```text
status
priority
search
sort
page
pageSize
```

Do not implement separate inconsistent filtering conventions for different endpoints.

---

# 37. RATE LIMITING

Rate limiting MUST be concrete, not merely conceptual.

Authentication-sensitive endpoints must have stricter limits than normal endpoints.

Rate limiting MUST protect at minimum:

- login
- password reset
- invitation operations
- file initialization
- export operations
- other abuse-sensitive endpoints

The numerical limits MUST follow `MASTER_SPECIFICATION.md`.

Do not replace defined limits with an unspecified "rate limiting enabled" implementation.

---

# 38. BRUTE FORCE PROTECTION

Authentication endpoints MUST include protection against brute-force attacks.

Appropriate mechanisms include:

- rate limiting
- temporary lockout
- progressive delays
- abuse detection
- CAPTCHA where explicitly required

Do not allow unlimited password attempts.

---

# 39. MFA

If MFA is enabled by the specification, it must be implemented particularly for privileged accounts.

MFA MUST be enforced server-side.

Do not implement fake frontend-only MFA.

---

# 40. ERROR HANDLING

The API MUST use centralized exception handling.

Expected behavior includes:

- consistent HTTP status codes
- structured error responses
- correlation IDs
- safe client-facing messages
- detailed server-side logs

Internal stack traces and secrets MUST NOT be exposed to clients.

---

# 41. LOGGING

Logging MUST be useful for debugging and auditing.

Do NOT log:

- passwords
- reset tokens
- refresh tokens
- sensitive personal data
- authorization secrets
- private file contents

Use correlation IDs to trace requests.

---

# 42. AUDIT LOGGING

Security-sensitive and business-critical actions must be auditable.

Examples:

- task creation
- assignment
- reassignment
- approval
- rejection
- submission review
- role changes
- file access where required
- account changes
- data export
- administrative actions

Audit logs MUST be protected from ordinary user modification.

---

# 43. NOTIFICATIONS

The notification system supports appropriate channels such as:

- in-app
- email

Notification preferences must be user-specific.

The system MUST respect user notification preferences where the specification permits opt-out.

Critical administrative/security notifications may follow mandatory rules defined by the specification.

---

# 44. REAL-TIME NOTIFICATIONS

SignalR may be used for real-time in-app notifications.

The frontend MUST gracefully handle:

- disconnected connections
- reconnects
- unavailable real-time services

Real-time functionality MUST NOT be the only mechanism for critical information.

Persistent notifications must remain available.

---

# 45. EMAIL ARCHITECTURE

Email must have its own Infrastructure layer:

```text
Infrastructure/Email/
```

The system MUST use an abstraction such as:

```text
IEmailService
IEmailProvider
```

Provider implementations may include:

```text
SMTP
SendGrid
Microsoft Graph
```

The business layer MUST NOT directly depend on a concrete email provider.

---

# 46. EMAIL DELIVERY IDEMPOTENCY

Email delivery must support idempotency.

`EmailDelivery` must allow the system to track:

- delivery identity
- status
- attempts
- provider information
- timestamps
- failures where appropriate

Retrying a background job MUST NOT unintentionally send duplicate emails.

---

# 47. BACKGROUND JOBS

Background jobs must handle asynchronous operations such as:

- deadline reminders
- overdue tasks
- recurring tasks
- email dispatch
- orphan file cleanup
- retention cleanup
- data exports
- marketplace claim expiration
- semester rollover

Jobs MUST be idempotent where applicable.

Failures MUST be logged.

Retry behavior MUST be bounded.

---

# 48. FILE UPLOAD BACKGROUND PROCESSING

Large file operations MUST NOT unnecessarily block request threads.

Use asynchronous processing where appropriate.

The API should return a job/upload status rather than holding a request open for large operations.

---

# 49. N+1 QUERY PREVENTION

The agent MUST actively prevent N+1 database queries.

Use:

- appropriate projections
- eager loading when justified
- batching
- optimized queries

Do not blindly use `Include()` everywhere.

Queries must load only the data needed.

---

# 50. DATABASE INDEXING

Frequently queried fields MUST have appropriate indexes.

Examples may include:

- Task status
- Task priority
- due dates
- assignment student
- task category
- semester
- notification user
- audit timestamps
- marketplace state

Indexes must be based on actual query patterns.

---

# 51. TRANSACTIONS

Business operations that modify multiple related records MUST use appropriate transactions.

Examples:

- assigning a task
- accepting a marketplace claim
- approving a request
- creating a submission with associated records
- role changes
- complex reassignment operations

Partial state MUST NOT be left behind when a critical operation fails.

---

# 52. VALIDATION

Validation must exist at the Application/API boundary.

The frontend is NOT a security boundary.

Every client-provided value MUST be validated server-side.

Validation must cover:

- required fields
- string lengths
- enum values
- file metadata
- dates
- IDs
- permissions
- business constraints

---

# 53. FRONTEND ARCHITECTURE

The frontend uses React + TypeScript.

Use the existing architecture:

```text
src/
├── app/
├── pages/
├── features/
├── components/
├── services/
├── hooks/
├── stores/
├── types/
├── utils/
├── constants/
├── schemas/
├── i18n/
└── styles/
```

Do not put all application logic into pages.

---

# 54. FRONTEND FEATURE ORGANIZATION

Feature-specific behavior belongs under:

```text
src/features/
```

Shared UI belongs under:

```text
src/components/
```

API communication belongs under:

```text
src/services/api/
```

Reusable validation schemas belong under:

```text
src/schemas/
```

---

# 55. FRONTEND VALIDATION

Use schema-based validation where specified.

Schemas should remain separate from UI components where practical.

Do not duplicate the same validation rules across dozens of components.

Backend validation remains authoritative.

---

# 56. INTERNATIONALIZATION

The frontend MUST use key-based internationalization.

Do not hardcode user-facing strings throughout components.

Use:

```text
src/i18n/
```

with locale resources such as:

```text
tr/
en/
```

Adding another language later should not require rewriting components.

---

# 57. FRONTEND TESTING

Frontend testing is mandatory.

The project MUST support:

- unit tests
- component tests
- integration tests

Use the configured test infrastructure, such as:

- Vitest
- React Testing Library

Do not omit frontend tests simply because backend tests exist.

---

# 58. BACKEND TESTING

Backend tests MUST include appropriate:

- unit tests
- integration tests
- architecture tests

Business-critical workflows require tests.

---

# 59. TEST QUALITY

Tests must test behavior rather than implementation details.

Avoid meaningless tests such as:

```text
assert service exists
assert property exists
```

Tests should validate actual outcomes.

---

# 60. AUTHORIZATION TESTS

Every major permission boundary should have authorization tests.

Test:

- authorized users
- unauthorized users
- forbidden roles
- resource ownership
- cross-user access attempts

Never assume authorization works merely because an attribute exists.

---

# 61. API TESTING

Important endpoints must have integration tests.

Especially test:

- authentication
- task assignment
- requests
- submissions
- marketplace race conditions
- file access
- role permissions
- pagination
- concurrency
- validation
- error handling

---

# 62. SECURITY TESTING

Test for:

- unauthorized access
- IDOR/resource access issues
- invalid tokens
- expired tokens
- brute-force protections
- file authorization
- malicious input
- role escalation

---

# 63. FILE SECURITY

Never trust:

- file extension
- client MIME type
- filename
- client-provided size

Server-side validation is mandatory.

File names should not be used directly as storage paths.

Prevent:

- path traversal
- unauthorized access
- executable upload risks
- storage key collisions

---

# 64. API RESPONSE CONSISTENCY

API responses must remain consistent.

Use common models for:

- pagination
- errors
- validation errors
- success responses where appropriate

Do not create arbitrary response formats per controller.

---

# 65. DTO RULES

Do not expose EF entities directly from controllers.

Use DTOs.

DTOs must expose only fields appropriate for the caller.

Sensitive/internal fields must not leak through API serialization.

---

# 66. ENTITY FRAMEWORK RULES

EF Core configurations belong in:

```text
Infrastructure/Persistence/Configurations/
```

Do not place extensive EF configuration inside domain entities.

Migrations belong in:

```text
Infrastructure/Persistence/Migrations/
```

---

# 67. DATABASE MIGRATIONS

Database schema changes MUST be represented through migrations.

Do not manually modify production schema without corresponding migration strategy.

Never delete existing migrations casually.

---

# 68. SEED DATA

Seed data must be deterministic.

Development seed data MUST NOT accidentally be used as production credentials.

Production secrets must come from secure configuration.

---

# 69. CONFIGURATION

Secrets MUST NOT be committed to source control.

Use:

- environment variables
- secret managers
- deployment configuration

Never commit:

- passwords
- JWT secrets
- SMTP credentials
- API keys
- storage credentials

---

# 70. ENVIRONMENT FILES

Separate environment examples should exist for:

```text
backend/.env.example
frontend/.env.example
```

The root `.env.example` may contain shared/container-level configuration.

Do not assume frontend and backend use identical environment variables.

---

# 71. DOCKER

The project supports containerized deployment.

Containers must be reproducible.

Do not rely on undocumented local machine dependencies.

Docker images should:

- install only required dependencies
- expose required ports
- use environment configuration
- support health checks
- avoid storing persistent application data inside ephemeral containers

---

# 72. STORAGE DEPLOYMENT

Production file storage should use an object-storage-compatible provider.

Local storage may be used for development/testing.

The application MUST use the storage abstraction rather than directly depending on filesystem APIs throughout the application.

---

# 73. HEALTH CHECKS

Health endpoints must distinguish:

- application health
- database health
- dependency health

Do not report healthy when the application cannot access required infrastructure.

---

# 74. OBSERVABILITY

Production deployment should provide:

- structured logs
- metrics
- health checks
- correlation IDs
- error tracking where configured

Monitoring configuration belongs under:

```text
infrastructure/monitoring/
```

---

# 75. BACKUP

Production database backups MUST be defined.

Backup strategy must specify:

- frequency
- retention
- storage
- restoration procedure
- verification

A backup that has never been tested for restoration should not be considered reliable.

---

# 76. DISASTER RECOVERY

Disaster recovery documentation must define:

- recovery process
- dependencies
- restore order
- expected recovery objectives where specified
- database restoration
- file storage restoration
- configuration restoration

---

# 77. ANNOUNCEMENTS

Announcements are a first-class feature.

They must support the fields and behavior defined by the specification, including concepts such as:

- title
- content
- expiration
- pinned state

Announcements require both:

- database representation
- API endpoints

Do not leave them as a frontend-only concept.

---

# 78. DEPARTMENT FILE STORAGE

Department Files are a first-class feature.

The shared storage area must support the organizational model defined in the specification.

Examples include:

```text
Logos
Templates
Guidelines
Forms
```

This is distinct from task submission files.

---

# 79. TASK COMMENTS AND CHECKLISTS

Task comments and checklist items are first-class domain concepts.

They require:

- entities
- persistence
- authorization
- API endpoints
- frontend UI
- tests

Do not implement them only as serialized JSON inside Task unless explicitly specified.

---

# 80. FEEDBACK

Feedback must have explicit domain/application/API support.

The system must follow the feedback rules defined in the specification.

Do not invent a rating model different from the specification.

---

# 81. SETTINGS

Settings must have their own Application feature area:

```text
Application/Settings/
```

It should contain appropriate:

```text
Commands/
Queries/
DTOs/
Validators/
```

Settings endpoints must be explicitly represented in the API architecture.

---

# 82. EXPORTS

Exports must have their own Application feature area:

```text
Application/Exports/
```

Large exports should support asynchronous processing where necessary.

Exports must respect:

- authorization
- KVKK
- retention
- file security
- cleanup

---

# 83. SEARCH / FILTER / SORT

The frontend must expose appropriate search/filter/sort functionality for collections defined by the specification.

Filtering must be performed server-side for large datasets.

Do not retrieve thousands of records and filter them only in the browser.

---

# 84. API DOCUMENTATION

OpenAPI documentation must remain synchronized with the implementation.

The canonical API documentation belongs under:

```text
docs/api/openapi.yaml
```

When adding/changing an endpoint, update API documentation accordingly.

---

# 85. PERMISSION MATRIX

Permission changes MUST remain synchronized with:

```text
docs/permissions/PERMISSION_MATRIX.md
```

If an endpoint introduces a new permission requirement, update the permission matrix.

---

# 86. IMPLEMENTATION PLAN

Large features should be implemented in logical stages.

Recommended sequence:

1. Domain
2. Persistence
3. Application
4. Infrastructure
5. API
6. Tests
7. Frontend API integration
8. Frontend UI
9. Integration tests
10. Documentation

Do not implement only the visible frontend and leave backend behavior mocked.

---

# 87. DEFINITION OF DONE

A feature is DONE only when:

- domain model exists where required,
- database model exists,
- migration exists,
- business logic exists,
- validation exists,
- authorization exists,
- API endpoint exists,
- API documentation exists,
- frontend integration exists,
- UI exists,
- notifications exist where required,
- audit behavior exists where required,
- tests exist,
- dependency injection is configured,
- error handling is implemented,
- logging is appropriate,
- edge cases are handled.

---

# 88. NO MOCKED PRODUCTION LOGIC

Do not leave production functionality implemented using:

- hardcoded arrays
- fake repositories
- mock API responses
- temporary in-memory collections
- placeholder services

unless the specification explicitly identifies them as development/test-only.

---

# 89. NO SILENT FEATURE REMOVAL

When modifying an existing feature:

- preserve existing behavior,
- preserve endpoints,
- preserve database relationships,
- preserve permissions,
- preserve tests,

unless the specification explicitly requires a breaking change.

If a change removes functionality, explicitly report it.

---

# 90. NO UNNECESSARY DEPENDENCIES

Do not add libraries merely for convenience.

Before adding a dependency:

1. check whether the repository already provides equivalent functionality,
2. check whether the dependency is compatible with the architecture,
3. determine whether it introduces security or maintenance risks.

Prefer existing project dependencies.

---

# 91. DATABASE PERFORMANCE

Avoid:

- loading entire tables,
- unnecessary tracking,
- N+1 queries,
- repeated identical queries,
- unbounded result sets,
- unnecessary joins.

Use pagination for large collections.

Use projections where appropriate.

---

# 92. ASYNC PROGRAMMING

Backend I/O operations should use asynchronous APIs.

Avoid unnecessary:

```csharp
.Result
.Wait()
```

Do not block threads waiting for asynchronous operations.

---

# 93. CANCELLATION

Long-running API and background operations should support cancellation where appropriate.

Pass cancellation tokens through application/infrastructure layers when meaningful.

---

# 94. TRANSACTIONAL BUSINESS OPERATIONS

When an operation updates several records representing one logical business action, treat it as one transactional unit.

Example:

```text
Marketplace Claim
    ↓
Create Claim
    ↓
Assign Task
    ↓
Update Listing
```

The system must not leave these in contradictory states.

---

# 95. FRONTEND ERROR HANDLING

The frontend must properly handle:

- validation errors
- unauthorized
- forbidden
- not found
- conflict
- rate limiting
- server errors
- network failures

A `409 Conflict` must not be displayed as a generic success/failure message when the user needs to refresh stale data.

---

# 96. FRONTEND AUTHORIZATION

Frontend role checks are for UX only.

Backend authorization remains authoritative.

Never assume:

```typescript
if (user.role === "ADMIN")
```

is enough to secure an operation.

---

# 97. ACCESSIBLE UI

UI components should follow reasonable accessibility practices:

- semantic elements
- keyboard navigation
- labels
- accessible form errors
- appropriate focus management
- readable status indicators

---

# 98. RESPONSIVE DESIGN

The system should work on:

- desktop
- tablet
- mobile

Do not assume the department staff will use only desktop computers.

---

# 99. DOCUMENTATION REQUIREMENTS

Important architecture decisions should be documented.

Update appropriate documentation under:

```text
docs/
```

Do not allow the implementation and documentation to drift apart.

---

# 100. CHANGE REPORTING

After making changes, report:

1. what changed,
2. which files changed,
3. why the change was necessary,
4. tests executed,
5. test results,
6. build results,
7. migrations created,
8. unresolved issues,
9. assumptions made.

Do not claim a test passed if it was not actually executed.

---

# 101. HALT ON LOOP

If the agent encounters the same:

- compilation failure,
- test failure,
- lint failure,
- migration failure,
- runtime failure

three times consecutively while attempting to fix it, the agent MUST STOP.

The agent must:

1. explain the persistent failure,
2. show the relevant error,
3. explain what was attempted,
4. identify what information is missing,
5. ask the user for guidance.

The agent MUST NOT endlessly make speculative changes.

---

# 102. VERIFICATION BEFORE COMPLETION

Before declaring a task complete, the agent MUST verify as much as possible:

```text
Build
↓
Unit Tests
↓
Integration Tests
↓
Architecture Tests
↓
Frontend Tests
↓
Lint
↓
Migration Validation
↓
Relevant API Tests
```

If a verification step cannot be executed, explicitly state that.

---

# 103. NO FALSE SUCCESS

Never say:

```text
Everything works.
```

unless the relevant verification was actually performed.

Use precise statements such as:

```text
Backend build passed.
Unit tests passed.
Frontend tests were not executed because dependencies are not installed.
```

---

# 104. SMALL SAFE CHANGES

Prefer small, reversible changes.

For large features:

- implement incrementally,
- compile frequently,
- test frequently,
- avoid huge unrelated rewrites.

---

# 105. AMBIGUITY RULE

If an important requirement is ambiguous and different interpretations would materially change:

- database schema,
- API behavior,
- authorization,
- business logic,
- data retention,
- security,
- user experience,

STOP and ask for clarification.

Do not invent a business decision.

---

# 106. ASSUMPTION RULE

If ambiguity is minor and does not materially affect the architecture, use the most conventional implementation.

Record the assumption in the final change report.

---

# 107. MIGRATION SAFETY

Never casually:

- drop production tables,
- delete columns,
- destroy existing data,
- recreate the entire database.

Destructive migrations require explicit justification and appropriate migration strategy.

---

# 108. SECURITY DEFAULTS

When uncertain, prefer the more secure behavior.

Examples:

- deny access rather than allow,
- private files rather than public files,
- validation rather than trust,
- expiration rather than indefinite tokens,
- audit rather than silent mutation.

Security decisions must still respect the specification.

---

# 109. API SECURITY

Every API endpoint must consider:

- authentication
- authorization
- input validation
- rate limiting
- resource ownership
- sensitive data exposure
- logging
- error handling

Public endpoints must be explicitly intentional.

---

# 110. DATA ACCESS CONTROL

Users may access only resources they are authorized to access.

Never assume that knowing an ID grants access.

Always enforce ownership/role/permission rules server-side.

---

# 111. FILE ACCESS CONTROL

A file endpoint MUST verify that the requesting user has access to the underlying resource.

Do not expose storage keys directly if that bypasses authorization.

---

# 112. NOTIFICATION DELIVERY

Notification delivery must be resilient.

A failed email should not necessarily prevent the underlying business transaction from succeeding.

Use asynchronous delivery where appropriate.

---

# 113. IDEMPOTENCY

Operations that may be retried MUST be designed to avoid duplicate side effects.

Examples:

- email sending
- exports
- background jobs
- invitation processing
- marketplace claims where appropriate
- webhook-like integrations

---

# 114. RETRY POLICY

Retries must be bounded.

Do not create infinite retry loops.

Transient failures may be retried according to an explicit policy.

Permanent failures must be recorded and surfaced.

---

# 115. BACKGROUND JOB SAFETY

Background jobs must:

- be idempotent where required,
- handle failures,
- avoid duplicate processing,
- log execution,
- respect cancellation,
- avoid overlapping execution when unsafe.

---

# 116. FRONTEND STATE

Do not duplicate server state unnecessarily across multiple state stores.

Use the project's selected server-state mechanism consistently.

Invalidate/refetch stale data after mutations where required.

---

# 117. FORM HANDLING

Forms must provide:

- validation
- loading states
- error states
- success feedback
- disabled states during submission where appropriate

Avoid duplicate submissions.

---

# 118. UI STATUS CONSISTENCY

Task/request/submission statuses must use the canonical backend enums and terminology.

Do not invent frontend-only status values.

---

# 119. ENUM CONSISTENCY

Canonical domain enums include concepts such as:

```text
UserRole
TaskStatus
TaskPriority
TaskDifficulty
AssignmentMode
AssignmentStatus
RequestStatus
SubmissionStatus
NotificationType
NotificationChannel
NotificationPreferenceType
FileStatus
EmailDeliveryStatus
SkillLevel
MarketplaceApprovalMode
MarketplaceClaimStatus
SemesterStatus
```

Frontend representations must remain synchronized with backend contracts.

---

# 120. FINAL AGENT BEHAVIOR

The agent's primary responsibilities are:

```text
UNDERSTAND
    ↓
VERIFY
    ↓
PLAN
    ↓
IMPLEMENT
    ↓
TEST
    ↓
REVIEW
    ↓
DOCUMENT
```

The agent MUST prioritize:

1. correctness,
2. security,
3. specification compliance,
4. data integrity,
5. maintainability,
6. testability,
7. performance,
8. developer convenience.

The agent MUST NOT prioritize speed of code generation over correctness.

The agent MUST NOT invent requirements.

The agent MUST NOT truncate code.

The agent MUST NOT silently remove features.

The agent MUST NOT bypass authorization.

The agent MUST NOT buffer 1 GB files in memory.

The agent MUST NOT ignore concurrency.

The agent MUST NOT return inconsistent pagination structures.

The agent MUST use UTC timestamps.

The agent MUST use appropriate soft deletion.

The agent MUST register dependencies in DI.

The agent MUST stop after repeated unresolved failures.

The agent MUST verify its work before declaring completion.

---

# FINAL DIRECTIVE

Treat `MASTER_SPECIFICATION.md` as the product constitution.

Treat this file as the implementation constitution.

When implementing any feature, always ask:

```text
What does the specification require?
What architectural layers are affected?
What database changes are required?
What permissions are required?
What API endpoints are required?
What frontend changes are required?
What notifications are required?
What audit/privacy implications exist?
What concurrency issues exist?
What tests are required?
What documentation must change?
What could break?
```

A feature is incomplete until all applicable answers have been addressed.

When uncertain about a material business decision:

STOP.

Do not guess.

Ask.
