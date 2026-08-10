# Department Student Workforce Management Platform

## Full-Stack Production-Ready Application Specification --- Final Revised Version

You are a **Senior Full-Stack Software Architect + Senior React
Developer + Senior ASP.NET Core Developer + DevOps Engineer**.

Build a **production-ready, modular, secure, scalable and deployable web
application** for managing student workers in a university Computer
Engineering department.

The platform must manage: - student workers - task assignment - task
status and deadlines - submissions and file versions - extension and
reassignment requests - student course schedules - availability -
workload - intelligent assignment recommendations - comments -
notifications - email reminders - announcements - department files -
recurring tasks - templates - analytics - audit logs - exports

The application is intended for real users and real student data. Do not
implement it as a simplified Todo application.

------------------------------------------------------------------------

# 1. PRODUCT NAME

Temporary name:

**Department Workforce Management System**

The name must be configurable so it can easily be changed later.

------------------------------------------------------------------------

# 2. PRODUCT GOAL

The system solves the following operational problem:

The department has student workers who receive different tasks. Each
student has: - different courses - different availability - different
skills - different current workload - different task history

Authorized department staff should be able to assign and monitor work
while avoiding unreasonable workload and schedule conflicts.

Students should be able to: - see their assigned tasks - accept tasks -
update task progress - upload deliverables - create submission
versions - comment - request an extension - request reassignment - see
their schedule - enter availability - see workload - receive in-app and
email notifications

The architecture must support future intelligent assignment and AI
services without requiring a rewrite.

------------------------------------------------------------------------

# 3. CORE PRODUCT PRINCIPLES

The application must:

1.  Be production-ready.
2.  Keep business logic out of controllers.
3.  Use strong backend authorization.
4.  Never trust frontend-only permission checks.
5.  Use DTOs rather than exposing EF entities directly.
6.  Use optimistic concurrency for mutable entities.
7.  Preserve assignment and submission history.
8.  Keep audit logs immutable.
9.  Treat file storage separately from database storage.
10. Support large files up to **1 GB per file**.
11. Validate uploads by size, extension and MIME/content signature where
    practical.
12. Avoid orphaned files.
13. Be designed for KVKK/privacy requirements.
14. Support database backup and disaster recovery.
15. Keep UI strings key-based so future i18n can be added without
    rewriting the UI.
16. Prefer real implementations over mock/fake APIs.
17. Never silently remove a requested feature.
18. Never hardcode secrets.
19. Avoid unnecessary MVP complexity while keeping clean extension
    points.

------------------------------------------------------------------------

# 4. TECHNOLOGY STACK

## Frontend

Use:

-   React
-   TypeScript
-   Vite
-   Tailwind CSS
-   React Router
-   TanStack Query
-   React Hook Form
-   Zod
-   Recharts or equivalent chart library
-   Lucide React
-   FullCalendar or an equivalent calendar component

Use a modern component-based architecture.

## Backend

Use:

-   C#
-   ASP.NET Core Web API
-   .NET 9 or the current stable .NET version
-   Entity Framework Core
-   PostgreSQL
-   ASP.NET Core Identity
-   JWT authentication
-   Refresh tokens
-   Role-based authorization
-   FluentValidation or equivalent
-   Swagger / OpenAPI
-   SignalR
-   Hangfire or equivalent background job system

## Infrastructure

Use:

-   Docker
-   Docker Compose for local development
-   Redis-compatible abstraction where useful
-   Azure Blob Storage or S3-compatible object storage
-   Managed PostgreSQL in production

------------------------------------------------------------------------

# 5. DEPLOYMENT

## Frontend

Deployable to:

**Vercel**

The project must include the configuration necessary for a Vercel
deployment.

## Backend

The backend must run in a production Docker container.

Prepare a multi-stage Dockerfile.

It should be deployable to container platforms such as:

-   Azure Container Apps
-   Railway
-   Render
-   another Docker-compatible managed platform

Do not couple the application to one provider.

## Database

Production must support managed PostgreSQL.

## Development

Provide:

``` text
docker-compose.yml
```

with services for:

``` text
api
postgres
redis
```

Redis may be optional during early development, but the architecture
must permit its use.

## Object storage

Create an abstraction such as:

``` text
IFileStorageService
```

with support for:

-   Azure Blob Storage
-   S3-compatible storage
-   local development storage

Do not store uploaded file binaries in PostgreSQL.

------------------------------------------------------------------------

# 6. USER ROLES

Roles:

``` text
ADMIN
TASK_MANAGER
REVIEWER
STUDENT
```

## ADMIN

Full system administration.

Can: - manage users - manage roles - manage students - manage settings -
manage tasks - assign/reassign tasks - manage requests - review
submissions - manage files - manage templates - manage recurring tasks -
view analytics - export reports - view audit logs - manage
announcements - manage skills and categories

## TASK_MANAGER

Can: - view students - view schedules and availability - view workload -
create tasks - edit tasks - assign tasks - reassign tasks - manage task
requests - view task submissions - comment - send reminders - view
operational dashboards

TASK_MANAGER must not automatically have unrestricted system
administration.

## REVIEWER

Can: - view submissions assigned for review - review submissions -
approve submissions - request revisions - add review comments

REVIEWER does not automatically manage students, system settings,
invitations or task assignment.

## STUDENT

Can: - view own profile - view own tasks - accept own tasks -
start/update own tasks - submit own work - upload files to own tasks -
view own submission history - comment on accessible tasks - request
extension - request reassignment - manage own schedule - manage own
availability - view own workload - view own notifications - browse and
claim marketplace tasks if enabled

Authorization must always be enforced on the backend.

------------------------------------------------------------------------

# 7. AUTHENTICATION AND INVITATION MODEL

The system is **invite-only**.

There is NO public student registration endpoint.

Students cannot freely create accounts.

An authorized ADMIN creates an invitation for a student.

The invitation contains a secure one-time token and an expiration date.

Required flows:

``` text
ADMIN
  ↓
Create invitation
  ↓
Student receives invitation email
  ↓
Student opens invitation
  ↓
Creates password / activates account
  ↓
Account becomes active
```

Use:

-   ASP.NET Core Identity
-   JWT access token
-   refresh token
-   refresh token rotation
-   secure password hashing
-   password reset flow
-   account activation/deactivation

Endpoints:

``` text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout

POST /api/v1/auth/invitations
GET  /api/v1/auth/invitations
POST /api/v1/auth/invitations/{id}/resend
POST /api/v1/auth/invitations/{id}/revoke
POST /api/v1/auth/accept-invitation

POST /api/v1/auth/forgot-password
POST /api/v1/auth/reset-password
```

Do NOT create:

``` text
POST /api/v1/auth/register
```

Public registration is intentionally disabled.

Design an authentication abstraction so university SSO / Microsoft Entra
ID can be added later.

------------------------------------------------------------------------

# 8. USER AND STUDENT MANAGEMENT

Student fields:

``` text
Id
FirstName
LastName
Email
Department
Role
IsActive
CreatedAt
UpdatedAt
```
is intentionally NOT stored anywhere in the system.
It is excluded from the Student entity, student import files, APIs, exports,
analytics, logs, and database schema. The system must operate fully without
collecting or processing student numbers.

Admin can: - view students - edit student information -
activate/deactivate accounts - view student tasks - view schedule - view
availability - view skills - view workload - view task history where
authorized

Prefer deactivation/soft deletion over hard deletion.

Endpoints:

``` text
GET  /api/v1/students
GET  /api/v1/students/{id}
PUT  /api/v1/students/{id}
POST /api/v1/students/{id}/activate
POST /api/v1/students/{id}/deactivate
```

------------------------------------------------------------------------

# 9. CURRENT USER PROFILE

Endpoints:

``` text
GET /api/v1/me
PUT /api/v1/me
PUT /api/v1/me/password
```

Student profile can contain:

``` text
Personal Information
Skills
Experience
Schedule
Availability
Current Tasks
Task History
Workload
Requests
Notifications
```

Users can edit only fields they are authorized to edit.

------------------------------------------------------------------------

# 10. SKILLS

Skill entity:

``` text
Id
Name
Description
CreatedAt
UpdatedAt
```

StudentSkill:

``` text
StudentId
SkillId
Level
```

Skill levels:

``` text
BEGINNER
INTERMEDIATE
ADVANCED
EXPERT
```

Examples:

``` text
React
Python
C#
ASP.NET
SQL
PostgreSQL
WordPress
Figma
Excel
Graphic Design
Research
Documentation
Social Media
```

Endpoints:

``` text
GET    /api/v1/skills
GET    /api/v1/skills/{id}
POST   /api/v1/skills
PUT    /api/v1/skills/{id}
DELETE /api/v1/skills/{id}

GET    /api/v1/students/{id}/skills
POST   /api/v1/students/{id}/skills
PUT    /api/v1/students/{id}/skills/{skillId}
DELETE /api/v1/students/{id}/skills/{skillId}
```

------------------------------------------------------------------------

# 11. TASK MANAGEMENT

Task entity:

``` text
Id
Title
Description
CategoryId
Priority
Difficulty
Status
AssignedStudentId
CreatedById
StartDate
Deadline
EstimatedDurationMinutes
CreatedAt
UpdatedAt
CompletedAt
RowVersion
```

IMPORTANT:

**EstimatedDurationMinutes is the canonical workload unit.**

All workload calculations must use minutes internally.

UI may display the value as hours/minutes, for example:

``` text
90 minutes → 1h 30m
```

Never mix minute-based and hour-based calculations.

Priority:

``` text
LOW
MEDIUM
HIGH
URGENT
```

Difficulty:

``` text
EASY
MEDIUM
HARD
```

Task endpoints:

``` text
GET    /api/v1/tasks
GET    /api/v1/tasks/{id}
POST   /api/v1/tasks
PUT    /api/v1/tasks/{id}
DELETE /api/v1/tasks/{id}

POST   /api/v1/tasks/{id}/assign
POST   /api/v1/tasks/{id}/reassign
POST   /api/v1/tasks/{id}/unassign
GET    /api/v1/tasks/{id}/assignment-history

POST   /api/v1/tasks/{id}/accept
POST   /api/v1/tasks/{id}/start
POST   /api/v1/tasks/{id}/submit

POST   /api/v1/tasks/{id}/approve
POST   /api/v1/tasks/{id}/request-revision

POST   /api/v1/tasks/{id}/reminder

POST   /api/v1/tasks/{id}/publish
POST   /api/v1/tasks/{id}/unpublish

GET    /api/v1/tasks/{id}/assignment-recommendations
POST   /api/v1/tasks/{id}/assignment-preview
```

Use optimistic concurrency.

If two authorized users edit the same task simultaneously, the second
conflicting update must return:

``` text
409 Conflict
```

rather than silently overwriting the first update.

Use EF Core row-version / concurrency-token semantics appropriate for
PostgreSQL.

------------------------------------------------------------------------

# 12. TASK CATEGORIES

Initial categories:

``` text
WEBSITE
EVENT
SOCIAL_MEDIA
RESEARCH
DOCUMENTATION
ADMINISTRATIVE
TECHNICAL
OTHER
```

Categories must be database-backed rather than hardcoded.

Admin can manage categories.

Provide CRUD endpoints following the same REST conventions as other
managed resources.

------------------------------------------------------------------------

# 13. TASK STATUS WORKFLOW

Primary workflow:

``` text
ASSIGNED
    ↓
ACCEPTED
    ↓
IN_PROGRESS
    ↓
SUBMITTED_FOR_REVIEW
    ↓
COMPLETED
```

Additional states:

``` text
INCOMPLETE
CANNOT_COMPLETE
CANCELLED
OVERDUE
```

Important distinction:

-   `SUBMITTED_FOR_REVIEW` means the student has submitted work.
-   `COMPLETED` means an authorized reviewer/manager has approved it.
-   `EXTENSION` and `REASSIGNMENT` are request states, not task states.

Students must not directly mark a task as `COMPLETED`.

------------------------------------------------------------------------

# 14. TASK CHECKLIST

Checklist item:

``` text
Id
TaskId
Title
IsCompleted
CompletedAt
CompletedBy
Order
```

Endpoints:

``` text
GET    /api/v1/tasks/{taskId}/checklist
POST   /api/v1/tasks/{taskId}/checklist
PUT    /api/v1/tasks/{taskId}/checklist/{itemId}
DELETE /api/v1/tasks/{taskId}/checklist/{itemId}

POST   /api/v1/tasks/{taskId}/checklist/{itemId}/complete
POST   /api/v1/tasks/{taskId}/checklist/{itemId}/uncomplete
```

------------------------------------------------------------------------

# 15. TASK DEPENDENCIES

TaskDependency:

``` text
Id
TaskId
DependsOnTaskId
```

Endpoints:

``` text
GET    /api/v1/tasks/{taskId}/dependencies
POST   /api/v1/tasks/{taskId}/dependencies
DELETE /api/v1/tasks/{taskId}/dependencies/{dependencyId}
GET    /api/v1/tasks/{taskId}/dependency-status
```

Prevent circular dependencies.

If dependencies are incomplete, either: - prevent starting the task,
or - show a clear warning according to configurable business rules.

------------------------------------------------------------------------

# 16. TASK REQUIRED SKILLS

Tasks can have required skills.

Endpoints:

``` text
GET    /api/v1/tasks/{taskId}/skills
POST   /api/v1/tasks/{taskId}/skills
DELETE /api/v1/tasks/{taskId}/skills/{skillId}
```

Required skills participate in assignment recommendations.

------------------------------------------------------------------------

# 17. TASK COMMENTS

Comment:

``` text
Id
TaskId
AuthorId
Content
CreatedAt
UpdatedAt
```

Endpoints:

``` text
GET    /api/v1/tasks/{taskId}/comments
POST   /api/v1/tasks/{taskId}/comments
PUT    /api/v1/tasks/{taskId}/comments/{commentId}
DELETE /api/v1/tasks/{taskId}/comments/{commentId}
```

Only authorized users may access task comments.

------------------------------------------------------------------------

# 18. FILE UPLOAD AND SUBMISSION

Student can upload deliverables to a task.

Do NOT store file binaries in PostgreSQL.

Store metadata in the database:

``` text
Id
TaskId
UploadedBy
FileName
StorageKey
FileSize
MimeType
FileExtension
ContentHash
Version
UploadedAt
DeletedAt
```

Store binary content in:

-   Azure Blob Storage
-   S3-compatible storage

## Maximum file size

**1 GB per file.**

Do not upload 1 GB files through a normal API memory buffer.

Use streaming and preferably direct-to-object-storage uploads using
signed URLs / multipart upload.

Recommended flow:

``` text
POST /api/v1/tasks/{taskId}/uploads/initiate
        ↓
Backend validates authorization + metadata
        ↓
Backend creates upload session / signed upload URL
        ↓
Browser uploads directly to object storage
        ↓
POST /api/v1/tasks/{taskId}/uploads/{uploadId}/complete
        ↓
Backend validates uploaded object
        ↓
SubmissionVersion created
```

Endpoints:

``` text
GET  /api/v1/tasks/{taskId}/submissions

POST /api/v1/tasks/{taskId}/uploads/initiate
POST /api/v1/tasks/{taskId}/uploads/{uploadId}/complete

GET  /api/v1/submissions/{submissionId}
GET  /api/v1/submissions/{submissionId}/versions
GET  /api/v1/submissions/{submissionId}/versions/{versionId}
GET  /api/v1/submissions/{submissionId}/download-url
GET  /api/v1/submissions/{submissionId}/versions/{versionId}/download-url
DELETE /api/v1/submissions/{submissionId}
```

Signed download URLs must be short-lived.

Never expose raw storage credentials to the frontend.

------------------------------------------------------------------------

# 19. FILE VALIDATION

Maximum:

``` text
1 GB
```

The exact accepted file types must be configurable.

Provide a safe initial allowlist suitable for department work, for
example:

``` text
PDF
DOC
DOCX
XLS
XLSX
PPT
PPTX
CSV
TXT
ZIP
PNG
JPG
JPEG
SVG
WEBP
MP4
MOV
```

Do not blindly trust the MIME type sent by the browser.

Validate: - extension - MIME type - file signature/content type where
practical - size - upload session - authorization

The application must never execute uploaded files.

For potentially dangerous file types, reject them by default.

------------------------------------------------------------------------

# 20. FILE VERSIONING

Every new submission creates a new version.

Example:

``` text
v1 → Submitted
v2 → Revision requested
v3 → Final
```

Never destroy historical submission versions simply because a new
version is uploaded.

If a task is reassigned, its existing submission history remains
associated with the task.

------------------------------------------------------------------------

# 21. ORPHAN FILE CLEANUP

The application must prevent object-storage orphan files.

Use states such as:

``` text
UPLOAD_PENDING
UPLOADED
CONFIRMED
DELETED
```

If an upload session is abandoned, a background job should clean it
after a configurable retention period.

If a database record is deleted, the corresponding object must be
deleted or scheduled for deletion.

Never immediately delete a file that is still referenced by an immutable
submission version.

Use background cleanup for failed deletion operations.

------------------------------------------------------------------------

# 22. TASK REVIEW

Student action:

``` text
POST /api/v1/tasks/{taskId}/submit
```

moves the task to:

``` text
SUBMITTED_FOR_REVIEW
```

Review actions:

``` text
POST /api/v1/tasks/{taskId}/approve
POST /api/v1/tasks/{taskId}/request-revision
```

Approval moves:

``` text
SUBMITTED_FOR_REVIEW → COMPLETED
```

Revision request moves:

``` text
SUBMITTED_FOR_REVIEW → IN_PROGRESS
```

Revision reason is mandatory.

A student cannot approve their own submission.

## REVIEWER vs TASK_MANAGER

Both roles may review/approve submissions for tasks they are authorized
to access.

However:

-   REVIEWER is primarily responsible for review.
-   TASK_MANAGER may approve operational tasks because they own task
    management.
-   ADMIN can do everything.

A user cannot approve their own submitted work.

Review history:

``` text
GET /api/v1/tasks/{taskId}/reviews
GET /api/v1/submissions/{submissionId}/reviews
```

------------------------------------------------------------------------

# 23. EXTENSION / REASSIGNMENT REQUEST SYSTEM

Use one request system.

Request:

``` text
Id
TaskId
RequestedBy
Type
Reason
RequestedDeadline
SuggestedStudentId
Status
CreatedAt
ReviewedAt
ReviewedBy
ReviewerComment
```

Types:

``` text
EXTENSION
REASSIGNMENT
```

Statuses:

``` text
PENDING
APPROVED
REJECTED
CANCELLED
```

Endpoints:

``` text
GET  /api/v1/requests
GET  /api/v1/requests/{id}
POST /api/v1/requests

POST /api/v1/requests/{id}/approve
POST /api/v1/requests/{id}/reject
POST /api/v1/requests/{id}/cancel

GET /api/v1/tasks/{taskId}/requests
```

## Extension

Student provides:

``` text
Current Deadline
Requested Deadline
Reason
```

When approved:

-   task deadline is updated
-   audit log is created
-   student is notified
-   email notification may be sent according to preferences

Do not allow a second pending extension request for the same task.

## Reassignment

Student may request to be removed from the task.

Reason is mandatory.

Student may optionally suggest another student.

Admin/Task Manager reviews and selects the new assignee.

When approved:

-   old assignment remains in history
-   new assignment is recorded
-   audit log is created
-   notifications are sent
-   task ownership changes atomically

Do not allow a second pending reassignment request for the same task.

------------------------------------------------------------------------

# 24. TASK ASSIGNMENT HISTORY

TaskAssignmentHistory:

``` text
Id
TaskId
StudentId
AssignedBy
AssignedAt
UnassignedAt
Reason
```

Endpoint:

``` text
GET /api/v1/tasks/{taskId}/assignment-history
```

Never overwrite assignment history.

------------------------------------------------------------------------

# 25. STUDENT SCHEDULE

Students enter their university course schedules by semester.

Student numbers are intentionally not stored or processed anywhere in the system.

Semester:

``` text
Id
Name
StartDate
EndDate
IsActive
```

Example:

``` text
2026 Fall
2027 Spring
```

CourseSchedule:

``` text
Id
StudentId
SemesterId
CourseName
CourseCode
DayOfWeek
StartTime
EndTime
Location
```

Endpoints:

``` text
GET    /api/v1/semesters
GET    /api/v1/semesters/{id}
POST   /api/v1/semesters
PUT    /api/v1/semesters/{id}
DELETE /api/v1/semesters/{id}

GET    /api/v1/me/schedule
POST   /api/v1/me/schedule

GET    /api/v1/students/{studentId}/schedule
POST   /api/v1/students/{studentId}/schedule

PUT    /api/v1/schedule/{id}
DELETE /api/v1/schedule/{id}
```

Students can edit their own schedule.

Authorized staff can view schedules.

------------------------------------------------------------------------

# 26. AVAILABILITY

Availability must be calculated using the student's course schedule.

Course time is automatically considered unavailable.

Students can also define additional unavailable periods.

Availability:

``` text
Id
StudentId
DayOfWeek
StartTime
EndTime
Status
Reason
```

Status:

``` text
AVAILABLE
UNAVAILABLE
```

Endpoints:

``` text
GET    /api/v1/me/availability
POST   /api/v1/me/availability

GET    /api/v1/students/{studentId}/availability

PUT    /api/v1/availability/{id}
DELETE /api/v1/availability/{id}
```

The system must prevent overlapping availability records where
appropriate.

------------------------------------------------------------------------

# 27. SCHEDULE CONFLICT DETECTION

When assigning a task, consider:

-   course schedule
-   explicit availability
-   existing tasks
-   estimated duration
-   deadline
-   current workload

Example:

``` text
Ayşe has class between 14:00–16:00.

Suggested available time:
16:00–18:00
```

The system should show conflicts clearly.

------------------------------------------------------------------------

# 28. WORKLOAD MANAGEMENT

The canonical unit is **minutes**.

Formula:

``` text
Estimated Workload Minutes =
sum(active task EstimatedDurationMinutes)
```

Available time is calculated from availability windows before the
relevant deadlines.

Workload percentage:

``` text
Workload % =
Estimated Active Work Minutes /
Available Work Minutes × 100
```

The implementation must document the exact time-window calculation.

Example:

``` text
Active Tasks: 3
Estimated Workload: 390 minutes
Available Time: 600 minutes
Workload: 65%
```

Display:

``` text
Ayşe       65%
Zeynep     40%
Mehmet     90%
Ali        20%
```

Workload above 100% must generate a warning.

Endpoints:

``` text
GET /api/v1/me/workload
GET /api/v1/students/{id}/workload
GET /api/v1/workload
GET /api/v1/workload/risks
```

Workload must be used for balancing, not as a punitive performance
score.

------------------------------------------------------------------------

# 29. INTELLIGENT TASK ASSIGNMENT

Initially use deterministic rule-based scoring.

Score:

``` text
40% Skill Match
30% Availability
20% Workload
10% Previous Experience
```

Example:

``` text
1. Ayşe — 92%
2. Zeynep — 84%
3. Mehmet — 61%
```

Expose reasons:

``` text
Skill Match: 95%
Availability: 90%
Workload: 45%
Deadline Risk: Low
```

Endpoint:

``` text
GET /api/v1/tasks/{taskId}/assignment-recommendations
```

Also support a non-mutating assignment preview:

``` text
POST /api/v1/tasks/{taskId}/assignment-preview
```

The preview must NOT assign the task.

Admin/Task Manager may manually select any eligible student.

The recommendation engine must not make irreversible assignments
automatically.

------------------------------------------------------------------------

# 30. MARKETPLACE / SELF-ASSIGNMENT

Optional but supported feature.

Some tasks may be published to a student marketplace.

Students can browse available tasks and claim them.

Endpoints:

``` text
GET /api/v1/marketplace/tasks
GET /api/v1/marketplace/tasks/{id}

POST /api/v1/marketplace/tasks/{id}/claim

GET /api/v1/marketplace/claims
GET /api/v1/marketplace/claims/{id}

POST /api/v1/marketplace/claims/{id}/approve
POST /api/v1/marketplace/claims/{id}/reject
POST /api/v1/marketplace/claims/{id}/cancel
```

Task publishing:

``` text
POST /api/v1/tasks/{id}/publish
POST /api/v1/tasks/{id}/unpublish
```

Marketplace tasks must still respect: - authorization - deadline -
workload - student eligibility - required skills

------------------------------------------------------------------------

# 31. CALENDAR

Admin calendar should show: - all tasks - deadlines - student
availability - course schedules where authorized

Student calendar should show: - courses - tasks - deadlines -
availability

Use a calendar library rather than manually implementing a full calendar
engine.

------------------------------------------------------------------------

# 32. OVERDUE TASK SYSTEM

A background job checks deadlines.

When:

``` text
now > deadline
```

and task is not completed/cancelled, mark it as:

``` text
OVERDUE
```

Notify the assigned student.

Do not repeatedly send the same overdue notification.

Use idempotency keys / notification records.

------------------------------------------------------------------------

# 33. SEND REMINDER

Admin/authorized Task Manager can press:

``` text
Send Reminder
```

This must:

1.  Create in-app notification.
2.  Send email to the student's registered email.
3.  Create audit log.
4.  Prevent accidental duplicate sends where appropriate.

Endpoint:

``` text
POST /api/v1/tasks/{taskId}/reminder
GET  /api/v1/tasks/{taskId}/reminders
```

Email service abstraction:

``` text
IEmailService
```

Possible implementations:

``` text
SendGridEmailService
MicrosoftGraphEmailService
```

Email example:

``` text
Subject:
Task Reminder – Website Update

Hello Ayşe,

Your task "Website Update" is overdue.

Deadline:
10 August 2026, 17:00

Please update your task status in the system.

View Task
```

------------------------------------------------------------------------

# 34. AUTOMATIC EMAIL NOTIFICATIONS

Support:

-   new task assigned
-   task deadline approaching
-   task overdue
-   revision requested
-   extension approved
-   extension rejected
-   reassignment approved
-   reassignment rejected
-   new comment
-   submission reviewed

Users must eventually be able to configure notification preferences.

Endpoints:

``` text
GET /api/v1/me/notification-preferences
PUT /api/v1/me/notification-preferences
```

------------------------------------------------------------------------

# 35. NOTIFICATION CENTER

Notification:

``` text
Id
UserId
Type
Title
Message
RelatedEntityType
RelatedEntityId
IsRead
CreatedAt
```

Endpoints:

``` text
GET    /api/v1/notifications
GET    /api/v1/notifications/unread-count
POST   /api/v1/notifications/{id}/read
POST   /api/v1/notifications/read-all
DELETE /api/v1/notifications/{id}
```

Use SignalR for real-time in-app notifications.

Hub:

``` text
/hubs/notifications
```

REST remains the source for persisted notification history.

------------------------------------------------------------------------

# 36. DEADLINE REMINDER BACKGROUND JOBS

Use Hangfire or an equivalent job system.

Default schedule:

``` text
24 hours before deadline
→ reminder

3 hours before deadline
→ urgent reminder

after deadline
→ mark overdue
```

Do not send duplicate notifications.

Background jobs must be idempotent.

------------------------------------------------------------------------

# 37. RECURRING TASKS

RecurringTask:

``` text
Id
TemplateId
Frequency
NextRunAt
IsActive
CreatedById
```

Endpoints:

``` text
GET    /api/v1/recurring-tasks
GET    /api/v1/recurring-tasks/{id}
POST   /api/v1/recurring-tasks
PUT    /api/v1/recurring-tasks/{id}
DELETE /api/v1/recurring-tasks/{id}

POST   /api/v1/recurring-tasks/{id}/activate
POST   /api/v1/recurring-tasks/{id}/deactivate
POST   /api/v1/recurring-tasks/{id}/run-now
```

Examples:

``` text
Every Monday → Website Check
Every month → Newsletter Preparation
```

Background jobs create actual Task records from the recurring
definition.

------------------------------------------------------------------------

# 38. TASK TEMPLATES

Template:

``` text
Id
Title
Description
CategoryId
DefaultPriority
DefaultDifficulty
EstimatedDurationMinutes
Checklist
RequiredSkills
CreatedById
CreatedAt
UpdatedAt
```

Endpoints:

``` text
GET    /api/v1/templates
GET    /api/v1/templates/{id}
POST   /api/v1/templates
PUT    /api/v1/templates/{id}
DELETE /api/v1/templates/{id}

POST   /api/v1/templates/{id}/create-task
```

------------------------------------------------------------------------

# 39. STUDENT DASHBOARD

Student dashboard:

``` text
My Tasks
Upcoming Deadlines
Overdue Tasks
Today's Schedule
Availability
Notifications
Workload
Pending Requests
Announcements
```

Task cards should clearly show: - priority - deadline - status -
estimated duration - progress/checklist - request status - submission
status

------------------------------------------------------------------------

# 40. ADMIN / TASK MANAGER DASHBOARD

Show:

``` text
Total Tasks
Active Tasks
Completed Tasks
Overdue Tasks
Pending Reviews
Pending Requests
Student Workload
Upcoming Deadlines
```

Include quick actions: - create task - assign task - review
submissions - view requests - send reminder

------------------------------------------------------------------------

# 41. STUDENT WORKLOAD ANALYTICS

Per student:

``` text
Tasks Completed
Average Completion Time
Overdue Count
Extension Count
Reassignment Count
Current Workload
Completion Rate
```

Do not use this as a punitive employee/student score.

Use it for: - workload balancing - resource planning - assignment
decisions

------------------------------------------------------------------------

# 42. DEPARTMENT ANALYTICS

Admin can view:

``` text
Total Tasks
Completed Tasks
Completion Rate
Average Completion Time
Overdue Rate
Extension Requests
Reassignment Requests
```

Charts:

``` text
Tasks by category
Tasks by status
Workload distribution
Completion trend
Overdue trend
```

Endpoints:

``` text
GET /api/v1/analytics/dashboard
GET /api/v1/analytics/tasks
GET /api/v1/analytics/workload
GET /api/v1/analytics/completion
GET /api/v1/analytics/overdue
GET /api/v1/analytics/requests
GET /api/v1/analytics/marketplace
GET /api/v1/analytics/students/{id}
```

------------------------------------------------------------------------

# 43. EXPORT

Admin can export:

``` text
CSV
Excel
PDF
```

Reports:

``` text
Task report
Student workload report
Semester report
```

Endpoints:

``` text
GET /api/v1/exports/tasks
GET /api/v1/exports/workload
GET /api/v1/exports/students
GET /api/v1/exports/semester
```

Use query parameters such as:

``` text
?format=csv
?format=xlsx
?format=pdf
```

Large exports should be generated asynchronously if necessary.

------------------------------------------------------------------------

# 44. AUDIT LOG

Critical actions must be audited.

AuditLog:

``` text
Id
UserId
Action
EntityType
EntityId
OldValue
NewValue
CreatedAt
IPAddress
```

Examples:

``` text
Admin created task
Task assigned to Ayşe
Ayşe uploaded file
Extension requested
Extension approved
Task submitted
Task completed
```

Endpoints:

``` text
GET /api/v1/audit-logs
GET /api/v1/audit-logs/{id}
```

Support filters:

``` text
userId
action
entityType
from
to
```

Audit logs must be append-only and must not be editable through the
application.

Do not store sensitive secrets or raw passwords in audit logs.

------------------------------------------------------------------------

# 45. DEPARTMENT FILE STORAGE

Separate department-level files from task submissions.

Example:

``` text
Department Files

Logos
Templates
Guidelines
Forms
Website Assets
Documents
```

Role-based access must apply.

Endpoints:

``` text
GET    /api/v1/files
GET    /api/v1/files/{id}
POST   /api/v1/files/upload/initiate
POST   /api/v1/files/upload/{uploadId}/complete
GET    /api/v1/files/{id}/download-url
DELETE /api/v1/files/{id}
```

Folder support may use:

``` text
GET    /api/v1/file-folders
POST   /api/v1/file-folders
PUT    /api/v1/file-folders/{id}
DELETE /api/v1/file-folders/{id}
```

Use the same 1 GB upload policy unless a more restrictive per-folder
setting is configured.

------------------------------------------------------------------------

# 46. ANNOUNCEMENTS

Announcement:

``` text
Id
Title
Content
CreatedBy
CreatedAt
ExpiresAt
IsPinned
```

Endpoints:

``` text
GET    /api/v1/announcements
GET    /api/v1/announcements/{id}
POST   /api/v1/announcements
PUT    /api/v1/announcements/{id}
DELETE /api/v1/announcements/{id}

POST   /api/v1/announcements/{id}/publish
POST   /api/v1/announcements/{id}/unpublish
```

Students see active announcements on the dashboard.

------------------------------------------------------------------------

# 47. SEARCH, FILTERING, SORTING AND PAGINATION

Task list must support:

``` text
?page=
&pageSize=
&search=
&studentId=
&status=
&priority=
&categoryId=
&difficulty=
&deadlineFrom=
&deadlineTo=
&sortBy=
&sortDirection=
```

Search: - title - description

Filters: - student - status - priority - category - deadline -
difficulty

Sort: - deadline - priority - created date - workload

Student list should support:

``` text
?search=
&skillId=
&isActive=
```

Use server-side pagination.

Return consistent pagination metadata.

------------------------------------------------------------------------

# 48. DATABASE DESIGN

At minimum consider:

``` text
ApplicationUser
StudentProfile
Role
Invitation
RefreshToken

Skill
StudentSkill

Semester
CourseSchedule
Availability

Task
TaskCategory
TaskAssignmentHistory
TaskChecklistItem
TaskDependency
TaskRequiredSkill
TaskComment

TaskSubmission
SubmissionVersion

TaskRequest
TaskReview

Notification
NotificationPreference

Announcement

TaskTemplate
RecurringTask

DepartmentFile
FileFolder

AuditLog
```

Use proper foreign keys and indexes.

Important indexes include:

``` text
Task.Deadline
Task.Status
Task.AssignedStudentId
Task.CategoryId
Notification.UserId
Notification.IsRead
AuditLog.EntityId
AuditLog.CreatedAt
CourseSchedule.StudentId
Availability.StudentId
TaskAssignmentHistory.TaskId
SubmissionVersion.SubmissionId
TaskRequest.TaskId
TaskRequest.Status
```

Add unique constraints where logically required.

------------------------------------------------------------------------

# 49. CONCURRENCY CONTROL

Production data must be protected from lost updates.

Use optimistic concurrency for: - Task - Student profile where
necessary - Requests - settings/configuration - other concurrently
edited entities

A conflicting update must return:

``` text
409 Conflict
```

with a clear error message.

The frontend should offer the user a refresh/reload path instead of
silently overwriting another user's changes.

Assignment and request approval operations should be transactional.

------------------------------------------------------------------------

# 50. SECURITY

Implement:

-   secure password hashing
-   JWT validation
-   refresh token rotation
-   role-based authorization
-   resource-level authorization
-   input validation
-   output encoding
-   file type validation
-   1 GB file size limit
-   rate limiting
-   CORS
-   HTTPS in production
-   secure headers
-   SQL injection protection through EF Core
-   XSS protection
-   CSRF considerations
-   secrets through environment variables
-   sensitive data masking in logs
-   signed short-lived file URLs
-   authorization before file download
-   authorization before every mutation

Never trust: - frontend role - frontend user ID - frontend task
ownership - client-provided file metadata - client-provided status
transitions

Validate all important state transitions on the backend.

------------------------------------------------------------------------

# 51. KVKK / PRIVACY

The system stores real student personal data.

Design with KVKK/privacy principles in mind.

At minimum: - collect only necessary personal data - do not collect or store student numbers - protect email addresses and profile
data - restrict access by role - avoid exposing student information to
other students - mask sensitive information in logs - define retention
rules - support deactivation - support appropriate
deletion/anonymization workflows where legally applicable - document
what data is stored and why - document who can access it - do not expose
personal data in analytics unnecessarily

Create a configurable retention policy.

Do not claim legal compliance automatically; the implementation should
provide technical controls supporting the department's legal/privacy
process.

------------------------------------------------------------------------

# 52. BACKUP AND DISASTER RECOVERY

Production PostgreSQL must use managed backup facilities or an
equivalent backup strategy.

Document:

-   backup frequency
-   retention period
-   restore procedure
-   disaster recovery procedure
-   RPO target
-   RTO target

Object storage should also have appropriate versioning/backup/lifecycle
capabilities where supported.

The README must explain how to restore: - database - application
configuration - object storage metadata/files

Do not treat backups as optional.

------------------------------------------------------------------------

# 53. EMAIL

Create:

``` text
IEmailService
```

Provider abstraction should support providers such as:

``` text
SendGrid
Microsoft Graph
SMTP-compatible provider
```

Do not hardcode provider credentials.

Email sending should be observable and retryable.

Background jobs should be used for non-critical transactional email
where appropriate.

------------------------------------------------------------------------

# 54. REAL-TIME NOTIFICATIONS

Use SignalR.

Hub:

``` text
/hubs/notifications
```

Events may include:

``` text
TaskAssigned
TaskUpdated
RequestCreated
RequestReviewed
SubmissionReviewed
CommentAdded
AnnouncementPublished
ReminderSent
```

Persist important notifications in PostgreSQL.

SignalR is for real-time delivery, not permanent storage.

------------------------------------------------------------------------

# 55. SETTINGS

System settings should be configurable.

Endpoints:

``` text
GET /api/v1/settings
PUT /api/v1/settings
```

Possible settings:

``` text
MAX_FILE_SIZE_BYTES
STUDENT_STORAGE_QUOTA
DEPARTMENT_STORAGE_QUOTA
WORKLOAD_WARNING_THRESHOLD
WORKLOAD_CRITICAL_THRESHOLD
REMINDER_24H_ENABLED
REMINDER_3H_ENABLED
OVERDUE_NOTIFICATIONS_ENABLED
RETENTION_PERIOD_DAYS
```

Only authorized administrators can modify system settings.

------------------------------------------------------------------------

# 56. API ENDPOINTS --- COMPLETE PLAN

Implement the following endpoint surface or a clearly equivalent REST
design.

## Authentication

``` text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout

POST /api/v1/auth/invitations
GET  /api/v1/auth/invitations
POST /api/v1/auth/invitations/{id}/resend
POST /api/v1/auth/invitations/{id}/revoke
POST /api/v1/auth/accept-invitation

POST /api/v1/auth/forgot-password
POST /api/v1/auth/reset-password
```

## Profile

``` text
GET /api/v1/me
PUT /api/v1/me
PUT /api/v1/me/password
GET /api/v1/me/notification-preferences
PUT /api/v1/me/notification-preferences
```

## Students

``` text
GET  /api/v1/students
GET  /api/v1/students/{id}
PUT  /api/v1/students/{id}
POST /api/v1/students/{id}/activate
POST /api/v1/students/{id}/deactivate

GET  /api/v1/students/{id}/skills
POST /api/v1/students/{id}/skills
PUT  /api/v1/students/{id}/skills/{skillId}
DELETE /api/v1/students/{id}/skills/{skillId}

GET /api/v1/students/{id}/schedule
GET /api/v1/students/{id}/availability
GET /api/v1/students/{id}/workload
GET /api/v1/students/{id}/feedback
```

## Skills

``` text
GET    /api/v1/skills
GET    /api/v1/skills/{id}
POST   /api/v1/skills
PUT    /api/v1/skills/{id}
DELETE /api/v1/skills/{id}
```

## Semesters

``` text
GET    /api/v1/semesters
GET    /api/v1/semesters/{id}
POST   /api/v1/semesters
PUT    /api/v1/semesters/{id}
DELETE /api/v1/semesters/{id}
```

## Schedule

``` text
GET    /api/v1/me/schedule
POST   /api/v1/me/schedule
PUT    /api/v1/schedule/{id}
DELETE /api/v1/schedule/{id}
```

## Availability

``` text
GET    /api/v1/me/availability
POST   /api/v1/me/availability
GET    /api/v1/students/{studentId}/availability
PUT    /api/v1/availability/{id}
DELETE /api/v1/availability/{id}
```

## Tasks

``` text
GET    /api/v1/tasks
GET    /api/v1/tasks/{id}
POST   /api/v1/tasks
PUT    /api/v1/tasks/{id}
DELETE /api/v1/tasks/{id}

POST   /api/v1/tasks/{id}/assign
POST   /api/v1/tasks/{id}/reassign
POST   /api/v1/tasks/{id}/unassign
GET    /api/v1/tasks/{id}/assignment-history

POST   /api/v1/tasks/{id}/accept
POST   /api/v1/tasks/{id}/start
POST   /api/v1/tasks/{id}/submit

POST   /api/v1/tasks/{id}/approve
POST   /api/v1/tasks/{id}/request-revision

POST   /api/v1/tasks/{id}/reminder
GET    /api/v1/tasks/{id}/reminders

POST   /api/v1/tasks/{id}/publish
POST   /api/v1/tasks/{id}/unpublish

GET    /api/v1/tasks/{id}/assignment-recommendations
POST   /api/v1/tasks/{id}/assignment-preview
```

## Checklist

``` text
GET    /api/v1/tasks/{taskId}/checklist
POST   /api/v1/tasks/{taskId}/checklist
PUT    /api/v1/tasks/{taskId}/checklist/{itemId}
DELETE /api/v1/tasks/{taskId}/checklist/{itemId}
POST   /api/v1/tasks/{taskId}/checklist/{itemId}/complete
POST   /api/v1/tasks/{taskId}/checklist/{itemId}/uncomplete
```

## Dependencies

``` text
GET    /api/v1/tasks/{taskId}/dependencies
POST   /api/v1/tasks/{taskId}/dependencies
DELETE /api/v1/tasks/{taskId}/dependencies/{dependencyId}
GET    /api/v1/tasks/{taskId}/dependency-status
```

## Required skills

``` text
GET    /api/v1/tasks/{taskId}/skills
POST   /api/v1/tasks/{taskId}/skills
DELETE /api/v1/tasks/{taskId}/skills/{skillId}
```

## Comments

``` text
GET    /api/v1/tasks/{taskId}/comments
POST   /api/v1/tasks/{taskId}/comments
PUT    /api/v1/tasks/{taskId}/comments/{commentId}
DELETE /api/v1/tasks/{taskId}/comments/{commentId}
```

## Uploads / submissions

``` text
GET  /api/v1/tasks/{taskId}/submissions

POST /api/v1/tasks/{taskId}/uploads/initiate
POST /api/v1/tasks/{taskId}/uploads/{uploadId}/complete

GET  /api/v1/submissions/{submissionId}
GET  /api/v1/submissions/{submissionId}/versions
GET  /api/v1/submissions/{submissionId}/versions/{versionId}

GET  /api/v1/submissions/{submissionId}/download-url
GET  /api/v1/submissions/{submissionId}/versions/{versionId}/download-url

DELETE /api/v1/submissions/{submissionId}
```

## Reviews

``` text
GET  /api/v1/tasks/{taskId}/reviews
GET  /api/v1/submissions/{submissionId}/reviews
POST /api/v1/tasks/{taskId}/approve
POST /api/v1/tasks/{taskId}/request-revision
```

## Requests

``` text
GET  /api/v1/requests
GET  /api/v1/requests/{id}
POST /api/v1/requests

POST /api/v1/requests/{id}/approve
POST /api/v1/requests/{id}/reject
POST /api/v1/requests/{id}/cancel

GET /api/v1/tasks/{taskId}/requests
```

## Marketplace

``` text
GET  /api/v1/marketplace/tasks
GET  /api/v1/marketplace/tasks/{id}
POST /api/v1/marketplace/tasks/{id}/claim

GET  /api/v1/marketplace/claims
GET  /api/v1/marketplace/claims/{id}

POST /api/v1/marketplace/claims/{id}/approve
POST /api/v1/marketplace/claims/{id}/reject
POST /api/v1/marketplace/claims/{id}/cancel
```

## Workload

``` text
GET /api/v1/me/workload
GET /api/v1/students/{id}/workload
GET /api/v1/workload
GET /api/v1/workload/risks
```

## Notifications

``` text
GET    /api/v1/notifications
GET    /api/v1/notifications/unread-count
POST   /api/v1/notifications/{id}/read
POST   /api/v1/notifications/read-all
DELETE /api/v1/notifications/{id}
```

## Announcements

``` text
GET    /api/v1/announcements
GET    /api/v1/announcements/{id}
POST   /api/v1/announcements
PUT    /api/v1/announcements/{id}
DELETE /api/v1/announcements/{id}

POST   /api/v1/announcements/{id}/publish
POST   /api/v1/announcements/{id}/unpublish
```

## Templates

``` text
GET    /api/v1/templates
GET    /api/v1/templates/{id}
POST   /api/v1/templates
PUT    /api/v1/templates/{id}
DELETE /api/v1/templates/{id}

POST   /api/v1/templates/{id}/create-task
```

## Recurring tasks

``` text
GET    /api/v1/recurring-tasks
GET    /api/v1/recurring-tasks/{id}
POST   /api/v1/recurring-tasks
PUT    /api/v1/recurring-tasks/{id}
DELETE /api/v1/recurring-tasks/{id}

POST   /api/v1/recurring-tasks/{id}/activate
POST   /api/v1/recurring-tasks/{id}/deactivate
POST   /api/v1/recurring-tasks/{id}/run-now
```

## Department files

``` text
GET    /api/v1/files
GET    /api/v1/files/{id}
POST   /api/v1/files/upload/initiate
POST   /api/v1/files/upload/{uploadId}/complete
GET    /api/v1/files/{id}/download-url
DELETE /api/v1/files/{id}
```

## File folders

``` text
GET    /api/v1/file-folders
POST   /api/v1/file-folders
PUT    /api/v1/file-folders/{id}
DELETE /api/v1/file-folders/{id}
```

## Feedback

``` text
GET  /api/v1/tasks/{taskId}/feedback
POST /api/v1/tasks/{taskId}/feedback
GET  /api/v1/students/{studentId}/feedback
```

## Audit

``` text
GET /api/v1/audit-logs
GET /api/v1/audit-logs/{id}
```

## Analytics

``` text
GET /api/v1/analytics/dashboard
GET /api/v1/analytics/tasks
GET /api/v1/analytics/workload
GET /api/v1/analytics/completion
GET /api/v1/analytics/overdue
GET /api/v1/analytics/requests
GET /api/v1/analytics/marketplace
GET /api/v1/analytics/students/{id}
```

## Exports

``` text
GET /api/v1/exports/tasks
GET /api/v1/exports/workload
GET /api/v1/exports/students
GET /api/v1/exports/semester
```

## Settings

``` text
GET /api/v1/settings
PUT /api/v1/settings
```

## Health

``` text
GET /health
GET /health/live
GET /health/ready
```

## SignalR

``` text
/hubs/notifications
```

REST endpoints and SignalR must have consistent authorization rules.

------------------------------------------------------------------------

# 57. API AUTHORIZATION MATRIX

Implement explicit backend authorization.

Examples:

  ---------------------------------------------------------------------------
  Operation                ADMIN   TASK_MANAGER       REVIEWER        STUDENT
  --------------- -------------- -------------- -------------- --------------
  Manage users               Yes             No             No             No

  Create task                Yes            Yes             No             No

  Assign task                Yes            Yes             No             No

  Review                     Yes            Yes            Yes             No
  submission                                                   

  Approve own                 No             No             No             No
  submission                                                   

  Manage system              Yes             No             No             No
  settings                                                     

  View own tasks             Yes            Yes            Yes            Yes

  View another        Authorized     Authorized  No by default             No
  student's                 only           only                
  private data                                                 

  Request                    Yes            Yes             No    Yes for own
  extension                                                              task

  Request                    Yes            Yes             No    Yes for own
  reassignment                                                           task

  View audit logs            Yes     Limited if             No             No
                                     explicitly                
                                        granted                

  Manage                     Yes       Optional             No             No
  announcements                                                
  ---------------------------------------------------------------------------

Do not assume every role can access every endpoint merely because it is
authenticated.

------------------------------------------------------------------------

# 58. ERROR HANDLING

Implement global exception handling middleware.

Use a consistent problem-details/error response.

Example:

``` json
{
  "success": false,
  "message": "Task not found.",
  "errors": []
}
```

Use appropriate status codes:

``` text
200
201
204
400
401
403
404
409
422
429
500
```

Use `409 Conflict` for optimistic concurrency conflicts and business
conflicts such as invalid state transitions where appropriate.

Never expose stack traces in production responses.

------------------------------------------------------------------------

# 59. ENVIRONMENT VARIABLES

Never commit secrets.

Example:

``` text
DATABASE_CONNECTION_STRING

JWT_SECRET
JWT_ISSUER
JWT_AUDIENCE

EMAIL_PROVIDER
SENDGRID_API_KEY
MICROSOFT_GRAPH_CLIENT_ID
MICROSOFT_GRAPH_CLIENT_SECRET

AZURE_STORAGE_CONNECTION_STRING
AZURE_STORAGE_CONTAINER

S3_ENDPOINT
S3_ACCESS_KEY
S3_SECRET_KEY
S3_BUCKET

REDIS_CONNECTION_STRING

FRONTEND_APP_URL
```

Frontend:

``` text
VITE_API_BASE_URL
```

Provide:

``` text
.env.example
```

Never put production secrets into the repository.

------------------------------------------------------------------------

# 60. DOCKER

Backend Dockerfile must use a multi-stage build:

``` text
SDK image
   ↓
restore
   ↓
build
   ↓
publish
   ↓
runtime image
```

Docker Compose:

``` text
api
postgres
redis
```

Add health checks.

Do not run production containers as root unless unavoidable.

------------------------------------------------------------------------

# 61. FRONTEND ARCHITECTURE

Suggested:

``` text
src/
├── api/
├── components/
│   ├── ui/
│   ├── tasks/
│   ├── students/
│   ├── calendar/
│   ├── dashboard/
│   ├── requests/
│   ├── notifications/
│   └── files/
├── pages/
├── layouts/
├── hooks/
├── contexts/
├── types/
├── utils/
├── validators/
├── i18n/
└── routes/
```

Use reusable components such as:

``` text
TaskCard
TaskStatusBadge
TaskPriorityBadge
TaskForm
TaskDetail
TaskChecklist
StudentCard
WorkloadBar
ScheduleCalendar
AvailabilityEditor
RequestModal
NotificationDropdown
FileUploader
SubmissionVersionList
```

------------------------------------------------------------------------

# 62. INTERNATIONALIZATION PREPARATION

The MVP may initially support English or Turkish, but do NOT hardcode
user-facing strings directly throughout components.

Use key-based strings:

``` text
task.status.completed
task.request.extension
notification.deadlineTomorrow
```

Use an i18n-ready abstraction such as `react-i18next` or an equivalent.

The architecture must permit adding Turkish/English and future languages
without rewriting components.

------------------------------------------------------------------------

# 63. STATE MANAGEMENT

Server state:

``` text
TanStack Query
```

Forms:

``` text
React Hook Form
```

Validation:

``` text
Zod
```

Avoid unnecessary global state.

Use SignalR only for real-time events, while TanStack Query remains the
primary source for server state.

------------------------------------------------------------------------

# 64. UI / UX

The UI should feel like:

**Student Workforce & Department Operations Platform**

not a Todo App.

Style:

-   clean
-   modern
-   minimal
-   academic / enterprise
-   accessible
-   responsive

Sidebar for staff:

``` text
Dashboard
My Tasks
Tasks
Calendar
Students
Schedule
Requests
Files
Announcements
Analytics
Notifications
Settings
```

Student sidebar:

``` text
Dashboard
My Tasks
Calendar
Schedule
Availability
Requests
Notifications
Profile
```

Dark mode may be supported.

Mobile must support: - task viewing - status updates - file upload -
comments - request creation

------------------------------------------------------------------------

# 65. TESTING

Backend:

-   unit tests
-   integration tests
-   API tests

Frontend:

-   component tests
-   integration tests
-   critical user-flow tests

Must test:

``` text
Authentication
Invitation
Password reset
Authorization
Task creation
Task assignment
Concurrent task update
Deadline handling
Extension request
Reassignment request
Submission
File upload
1 GB upload flow
Submission versioning
Review
Revision
Workload calculation
Availability calculation
Conflict detection
Marketplace claim
Reminder email
SignalR notification
Overdue task
Orphan file cleanup
Audit logging
```

Do not only test happy paths.

------------------------------------------------------------------------

# 66. SEED DATA

Development seed:

``` text
1 Admin
2 Task Managers
1 Reviewer
5 Students
10 Skills
20 Tasks
Multiple schedules
Availability
Notifications
Requests
Templates
Recurring tasks
Announcements
```

Use realistic dummy data.

Development-only credentials must be documented.

Never use seed passwords in production.

------------------------------------------------------------------------

# 67. API DOCUMENTATION

Enable Swagger/OpenAPI.

Document:

-   endpoint descriptions
-   request models
-   response models
-   validation rules
-   authorization requirements
-   example requests
-   example responses
-   error responses

------------------------------------------------------------------------

# 68. README

Create a detailed README containing:

``` text
Project Overview
Features
Architecture
Tech Stack
Requirements
Installation
Environment Variables
Database Setup
Docker Setup
Running Locally
Testing
Deployment
API Documentation
Folder Structure
Security
Privacy / KVKK Considerations
Backup / Disaster Recovery
File Storage
Background Jobs
SignalR
Future Improvements
```

------------------------------------------------------------------------

# 69. GIT STRUCTURE

Monorepo:

``` text
department-workforce/
├── frontend/
├── backend/
├── docker-compose.yml
├── docs/
├── .env.example
├── .gitignore
└── README.md
```

GitHub Actions:

``` text
Build
Test
Lint
```

Frontend deployable to Vercel.

Backend deployable as Docker image.

------------------------------------------------------------------------

# 70. DEVELOPMENT PHASES

Do not generate the entire application uncontrolled in one step.

## PHASE 1 --- Foundation

-   repository
-   frontend
-   backend
-   PostgreSQL
-   Docker
-   Identity
-   JWT
-   refresh tokens
-   invitation system
-   password reset
-   database migrations
-   basic authorization
-   health endpoints

## PHASE 2 --- Student Management

-   student profiles
-   roles
-   skills
-   activation/deactivation

## PHASE 3 --- Tasks

-   task CRUD
-   categories
-   assignment
-   statuses
-   deadlines
-   priorities
-   checklist
-   dependencies
-   required skills
-   comments
-   optimistic concurrency

## PHASE 4 --- Submissions

-   upload sessions
-   object storage
-   1 GB upload
-   multipart/direct upload
-   submission versioning
-   download URLs
-   review
-   revision

## PHASE 5 --- Schedule

-   semesters
-   courses
-   schedule
-   availability
-   conflict detection
-   calendar

## PHASE 6 --- Workload

-   workload calculation
-   workload dashboard
-   availability-based capacity
-   assignment scoring
-   recommendation explanation

## PHASE 7 --- Requests

-   extension
-   reassignment
-   approval/rejection
-   notifications
-   audit

## PHASE 8 --- Notifications

-   in-app notifications
-   SignalR
-   email
-   reminders
-   overdue detection
-   notification preferences

## PHASE 9 --- Templates and Recurring Tasks

-   templates
-   recurring tasks
-   background jobs

## PHASE 10 --- Marketplace

-   publish/unpublish
-   student claim
-   approval/rejection
-   workload-aware eligibility

## PHASE 11 --- Analytics

-   dashboard
-   charts
-   workload analytics
-   reports
-   exports

## PHASE 12 --- Department Storage and Announcements

-   department files
-   folders
-   announcements

## PHASE 13 --- Audit / Privacy / Operations

-   audit logs
-   retention
-   backup documentation
-   disaster recovery documentation
-   orphan file cleanup

## PHASE 14 --- AI Preparation

-   AI service abstraction
-   assignment recommendation interface
-   future Python FastAPI service contract
-   no mandatory ML in MVP

------------------------------------------------------------------------

# 71. FUTURE AI ARCHITECTURE

Future architecture:

``` text
React
  ↓
ASP.NET Core
  ↓
Python FastAPI AI Service
```

Potential capabilities:

``` text
Task classification
Task duration prediction
Workload prediction
Assignment recommendation
Deadline risk prediction
Natural-language task creation
Department assistant
```

The MVP must not depend on AI.

The deterministic recommendation engine must remain functional without
AI.

------------------------------------------------------------------------

# 72. BUSINESS RULES

## Assignment

Inactive students cannot receive tasks.

## Deadline

Do not allow invalid deadlines.

If business rules permit past deadlines for administrative purposes,
require an explicit warning/override.

## Extension

Only one pending extension request per task.

Requested deadline must be later than current deadline.

## Reassignment

Only one pending reassignment request per task.

## Review

Student cannot approve their own submission.

## Authorization

Student cannot access another student's private tasks, schedule,
availability, workload or files.

## Schedule

Course periods count as unavailable.

## Workload

EstimatedDurationMinutes is always the workload source.

## Dependencies

Circular dependencies are forbidden.

## Audit

Critical state changes are audited.

## File upload

Maximum file size is 1 GB.

## File deletion

Do not delete files still referenced by immutable submission versions.

------------------------------------------------------------------------

# 73. BACKUP / DISASTER RECOVERY REQUIREMENTS

Production deployment must document:

``` text
Database backup schedule
Database backup retention
Object storage retention/versioning
Restore process
RPO
RTO
Disaster recovery procedure
```

Use managed PostgreSQL backup capabilities where available.

Test restoration in a non-production environment.

------------------------------------------------------------------------

# 74. OBSERVABILITY

Implement:

-   structured logging
-   correlation/request IDs
-   health checks
-   error logging
-   background job monitoring
-   email delivery logging
-   upload lifecycle logging
-   audit logs

Never log: - passwords - JWT secrets - refresh tokens - storage
credentials - full sensitive personal data unnecessarily

------------------------------------------------------------------------

# 75. CODE QUALITY

Code must be:

-   readable
-   maintainable
-   strongly typed
-   modular
-   SOLID
-   DRY
-   separated by responsibility

Use: - DTOs - validators - services - interfaces - dependency
injection - enums/constants

Avoid: - magic strings - duplicated business logic - N+1 queries -
synchronous blocking - giant controllers - giant React components

Use:

``` text
async/await
CancellationToken
structured logging
pagination
projection
proper indexes
```

------------------------------------------------------------------------

# 76. IMPORTANT DEVELOPMENT RULES

When implementing:

1.  Build architecture first.
2.  Build database entities second.
3.  Build migrations third.
4.  Build application services.
5.  Build API endpoints.
6.  Build frontend pages/components.
7.  Integrate frontend and backend.
8.  Add tests.
9.  Add Docker.
10. Add deployment configuration.
11. Add observability.
12. Verify security.
13. Verify backup/restore documentation.

Every phase must leave the application runnable.

Do not: - leave critical TODOs - create fake APIs - create placeholder
implementations for required features - hardcode secrets - silently
remove requirements - silently simplify the system

------------------------------------------------------------------------

# 77. FIRST RESPONSE / FIRST TASK

Do NOT immediately generate the entire application.

First provide:

1.  Final architecture
2.  Final folder structure
3.  Database ERD description
4.  Entity list and relationships
5.  Complete API endpoint plan
6.  Authorization matrix
7.  Development phases
8.  Docker architecture
9.  Deployment architecture
10. File upload/storage architecture
11. Background job architecture
12. SignalR architecture
13. Backup/disaster-recovery plan
14. KVKK/privacy considerations

Then wait for approval.

After approval, start **PHASE 1 implementation**.

For every completed phase, report:

-   created files
-   changed files
-   database migrations
-   API endpoints added
-   frontend features added
-   commands to run
-   tests executed
-   test results
-   environment variables required
-   known limitations

Do not start the next phase until the current phase is working.

------------------------------------------------------------------------

# 78. FINAL EXPECTED USER FLOW

The main flow must work as follows:

``` text
ADMIN / TASK MANAGER CREATES TASK
                ↓
SYSTEM ANALYZES ELIGIBLE STUDENTS
                ↓
SYSTEM RECOMMENDS BEST STUDENT
                ↓
ADMIN / TASK MANAGER ASSIGNS TASK
                ↓
STUDENT RECEIVES IN-APP + EMAIL NOTIFICATION
                ↓
STUDENT ACCEPTS TASK
                ↓
STUDENT SEES TASK + DEADLINE + CHECKLIST
                ↓
STUDENT SCHEDULE / AVAILABILITY IS VISIBLE TO AUTHORIZED STAFF
                ↓
STUDENT WORKS ON TASK
                ↓
STUDENT UPLOADS FILE
                ↓
SUBMISSION VERSION CREATED
                ↓
STUDENT SUBMITS FOR REVIEW
                ↓
REVIEWER / TASK MANAGER / ADMIN REVIEWS
                ↓
        ┌─────────────────────┐
        │                     │
        ↓                     ↓
    APPROVED             REQUEST CHANGES
        │                     │
        ↓                     ↓
   COMPLETED             IN_PROGRESS
```

If a problem occurs:

``` text
STUDENT
   ↓
EXTENSION REQUEST
   OR
REASSIGNMENT REQUEST
   ↓
AUTHORIZED MANAGER REVIEWS
   ↓
APPROVE / REJECT
```

Deadline reminders:

``` text
24 HOURS BEFORE
       ↓
EMAIL + IN-APP REMINDER

3 HOURS BEFORE
       ↓
URGENT REMINDER

DEADLINE PASSED
       ↓
OVERDUE

ADMIN / TASK MANAGER
       ↓
SEND REMINDER
       ↓
IN-APP + EMAIL + AUDIT LOG
```

------------------------------------------------------------------------

# 79. FINAL DESIGN PRINCIPLE

# 81. API VERSIONING, PAGINATION, SEARCH, FILTERING AND RATE LIMITING

All public API routes must use an explicit version prefix:

```text
/api/v1/...
```

Do not expose unversioned `/api/...` routes in production.

Future breaking changes may be introduced under `/api/v2/...`.

## Standard pagination

Every paginated endpoint must return the same envelope:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 137,
  "totalPages": 7,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

Use server-side pagination for all collection endpoints that can grow.

## Search, filtering and sorting

Task listing must support:

```text
GET /api/v1/tasks
?page=1
&pageSize=20
&search=
&status=
&priority=
&difficulty=
&categoryId=
&studentId=
&deadlineFrom=
&deadlineTo=
&sortBy=
&sortDirection=
```

Search:
- title
- description

Filters:
- student
- status
- priority
- category
- difficulty
- deadline

Sort:
- deadline
- priority
- created date
- workload

Student listing should support:

```text
GET /api/v1/students
?search=
&skillId=
&isActive=
&page=
&pageSize=
```

Equivalent filtering/pagination conventions must be used consistently across requests, notifications, files, announcements, audit logs and analytics collections.

## Rate limiting

Implement numeric rate limits.

Initial defaults:

```text
Authenticated API users:
120 requests/minute/user

Anonymous endpoints:
30 requests/minute/IP

Login:
5 failed attempts/15 minutes/IP + account

Forgot password:
3 requests/hour/account + IP

Invitation resend:
5 requests/hour/invitation

Upload initiation:
20 requests/minute/user
```

Return:

```text
429 Too Many Requests
```

with a `Retry-After` header when appropriate.

Rate limits must be configurable per environment.

## Brute-force protection

ASP.NET Core Identity account lockout must be enabled for password authentication.

For example:

```text
5 failed login attempts
→ temporary lockout
```

The exact lockout duration must be configurable.

Do not reveal whether an email address exists during forgot-password requests.

## CORS

Use an explicit production origin allowlist.

Never use unrestricted:

```text
AllowAnyOrigin()
```

in production.

## API contract consistency

Every endpoint must define:

```text
Request DTO
Response DTO
Validation rules
Authorization requirements
Status codes
Error response
Pagination behavior where applicable
```

EF Core entities must never be exposed directly as public API response models.

---

# 82. PRODUCTION COMPLETENESS & REMAINING REQUIREMENTS

The following requirements are mandatory additions to the complete system specification.

## 82.1 User and role management

ADMIN must be able to manage users and roles.

```text
GET  /api/v1/users
GET  /api/v1/users/{id}
PUT  /api/v1/users/{id}/role
POST /api/v1/users/{id}/activate
POST /api/v1/users/{id}/deactivate
```

Role changes must be audited.

Students remain invite-only; there is no public registration.

## 82.2 Student import

ADMIN can import students from CSV/XLSX.

Flow:

```text
CSV/XLSX
   ↓
Upload
   ↓
Validation
   ↓
Duplicate detection
   ↓
Preview
   ↓
Admin confirmation
   ↓
Students created
   ↓
Invitations generated
```

Endpoints:

```text
POST /api/v1/students/import/preview
POST /api/v1/students/import/confirm
```

The import must never directly mutate production data before confirmation.

## 82.3 Department file storage

Department files are separate from task submissions.

Example folders:

```text
Logos
Templates
Guidelines
Forms
Website Assets
Documents
```

Entities:

```text
DepartmentFile
FileFolder
```

DepartmentFile:

```text
Id
FolderId
UploadedById
FileName
StorageKey
FileSize
MimeType
FileExtension
ContentHash
CreatedAt
DeletedAt
```

Endpoints:

```text
GET    /api/v1/files
GET    /api/v1/files/{id}
POST   /api/v1/files/upload/initiate
POST   /api/v1/files/upload/{uploadId}/complete
GET    /api/v1/files/{id}/download-url
DELETE /api/v1/files/{id}

GET    /api/v1/file-folders
POST   /api/v1/file-folders
PUT    /api/v1/file-folders/{id}
DELETE /api/v1/file-folders/{id}
```

Department files must obey the same authorization, storage, validation and orphan-cleanup rules as task files.

## 82.4 Announcements

Announcement:

```text
Id
Title
Content
CreatedById
CreatedAt
UpdatedAt
ExpiresAt
IsPinned
IsPublished
```

Endpoints:

```text
GET    /api/v1/announcements
GET    /api/v1/announcements/{id}
POST   /api/v1/announcements
PUT    /api/v1/announcements/{id}
DELETE /api/v1/announcements/{id}

POST   /api/v1/announcements/{id}/publish
POST   /api/v1/announcements/{id}/unpublish
```

Students see active announcements on their dashboard.

## 82.5 TaskRequiredSkill entity

Define the entity explicitly:

```text
TaskRequiredSkill
Id
TaskId
SkillId
MinimumLevel
CreatedAt
```

`MinimumLevel` uses:

```text
BEGINNER
INTERMEDIATE
ADVANCED
EXPERT
```

Add a unique constraint on:

```text
TaskId + SkillId
```

Assignment recommendations must consider both required skills and minimum levels.

## 82.6 Submission download authorization

A user must never gain access to a file merely by knowing its ID.

Before issuing a download URL, verify:

```text
Authenticated user
      ↓
Task access
      ↓
Submission access
      ↓
File access
```

Only then generate a short-lived signed download URL.

## 82.7 Feedback

Feedback is task-related feedback, not a punitive employee-rating system.

Entity:

```text
Feedback
Id
TaskId
StudentId
CreatedById
Rating
Comment
CreatedAt
UpdatedAt
```

Endpoints:

```text
GET  /api/v1/tasks/{taskId}/feedback
POST /api/v1/tasks/{taskId}/feedback
GET  /api/v1/students/{studentId}/feedback
```

If rating is enabled, use a bounded numeric scale such as 1–5 and document its meaning.

## 82.8 Task cancellation

Only:

```text
ADMIN
TASK_MANAGER
```

may cancel a task.

Endpoint:

```text
POST /api/v1/tasks/{id}/cancel
```

A cancellation reason is mandatory.

Cancelled tasks:

- remain in task history
- remain in audit logs
- do not contribute to active workload
- cannot be submitted
- cannot be approved
- cannot be claimed from the marketplace

## 82.9 Task status transition matrix

Backend must enforce valid transitions.

```text
ASSIGNED → ACCEPTED
ACCEPTED → IN_PROGRESS
IN_PROGRESS → SUBMITTED_FOR_REVIEW
SUBMITTED_FOR_REVIEW → COMPLETED
SUBMITTED_FOR_REVIEW → IN_PROGRESS

ASSIGNED → CANCELLED
ACCEPTED → CANCELLED
IN_PROGRESS → CANCELLED
```

Invalid transitions must return a suitable business error, normally `409 Conflict` or `422 Unprocessable Entity`.

Frontend hiding buttons is not sufficient.

## 82.10 Marketplace concurrency

Two students claiming the same marketplace task simultaneously must never both succeed.

Use:

- database transaction
- row-level/concurrency protection
- unique business constraint where appropriate

The losing request must receive:

```text
409 Conflict
```

## 82.11 Marketplace expiration

Marketplace publication may have a claim deadline.

Marketplace claim state:

```text
PENDING
APPROVED
REJECTED
CANCELLED
EXPIRED
```

Include:

```text
ExpiresAt
ClaimedAt
ApprovedAt
RejectedAt
```

If no student claims the task before expiration, the task becomes `EXPIRED` and remains available for manual reassignment/republication.

## 82.12 Semester rollover

Historical schedules must not be deleted when a semester ends.

Each schedule belongs to a semester.

Completed semesters become archived/inactive.

Only the active semester is used for current planning by default.

Historical schedule data remains available to authorized staff.

Availability must use an explicit date/semester scope so old availability does not contaminate future workload calculations.

## 82.13 Timezone strategy

Store timestamps consistently in UTC where the value represents an instant in time.

Use:

```text
Europe/Istanbul
```

as the initial department display timezone, but make the timezone configurable.

Course schedules and recurring local-time rules must preserve their intended local-time semantics.

Frontend converts UTC timestamps to the configured display timezone.

Never rely on server local time.

## 82.14 File quotas

The existing **1 GB per-file maximum** remains mandatory.

Additionally, configurable total quotas must exist:

```text
STUDENT_STORAGE_QUOTA_BYTES
DEPARTMENT_STORAGE_QUOTA_BYTES
```

Quota checks must happen before upload initiation.

## 82.15 File security

File handling must include:

- extension allowlist
- MIME validation
- content/signature validation where practical
- executable-file rejection
- archive safety checks
- ZIP bomb protection
- optional antivirus/malware scanning integration
- signed URLs
- upload timeout
- abandoned upload cleanup

Uploaded files must never be executed by the application.

## 82.16 Upload/storage consistency

Use an upload lifecycle:

```text
UPLOAD_PENDING
UPLOADED
CONFIRMED
FAILED
DELETED
```

Handle these failure cases:

```text
DB record created → storage upload fails
Storage upload succeeds → DB confirmation fails
```

A scheduled cleanup process must reconcile unconfirmed storage objects.

## 82.17 Idempotency

Critical state-changing operations must be idempotent where duplicate requests are possible.

At minimum:

```text
Marketplace claim
Assignment
Reassignment approval
Extension approval
Submission upload completion
Reminder creation
Notification creation
```

Use idempotency keys and/or unique database constraints where appropriate.

## 82.18 Notification idempotency

The same reminder must not be sent twice.

Use a deterministic idempotency key such as:

```text
TASK_{taskId}_DEADLINE_24H
TASK_{taskId}_DEADLINE_3H
TASK_{taskId}_OVERDUE
```

with appropriate uniqueness constraints.

## 82.19 Email delivery tracking and retries

Email records should support:

```text
QUEUED
SENT
FAILED
```

Provider integrations may additionally support:

```text
DELIVERED
BOUNCED
```

Failed transient emails must be retried with exponential backoff.

Permanent failures must be observable by administrators.

## 82.20 Background job reliability

Background jobs must be:

- idempotent
- retryable
- observable
- safe to execute more than once

Jobs include:

```text
Deadline reminders
Overdue detection
Email sending
Recurring task creation
Orphan file cleanup
Data export
Retention cleanup
```

## 82.21 Session/device management

Users should be able to view and revoke active sessions.

```text
GET  /api/v1/me/sessions
POST /api/v1/me/sessions/{id}/revoke
POST /api/v1/me/sessions/revoke-all
```

Refresh-token records must be associated with sessions/devices where practical.

## 82.22 MFA

Prepare MFA infrastructure, especially for:

```text
ADMIN
TASK_MANAGER
```

Support TOTP/authenticator applications or a future university SSO provider.

MFA must be configurable rather than hardcoded into business logic.

## 82.23 Personal data export

Students must be able to request an export of their own stored data.

```text
GET /api/v1/me/data-export
```

The export may include:

```text
Profile
Tasks
Submission metadata
Schedule
Availability
Requests
Notifications
Skills
```

Large exports should use a background job and an expiring download URL.

## 82.24 Data retention and deletion matrix

Define retention behavior for:

```text
Student
Task
Comment
Submission
DepartmentFile
Announcement
Notification
AuditLog
```

Use soft deletion or anonymization where historical integrity requires it.

Do not delete records needed to preserve audit or submission history without an explicit retention policy.

## 82.25 Audit scope

Audit critical security and business operations:

```text
Login
Failed login
Password reset
Invitation created
Invitation revoked
Role changed
User activated/deactivated
Task created
Task assigned
Task reassigned
Task cancelled
Submission uploaded
Submission deleted
Review completed
Request approved/rejected
File downloaded
Settings changed
```

Do not audit every ordinary GET request.

Never store passwords, tokens or secrets in audit logs.

## 82.26 Database transactions

The following operations must be transactional:

```text
Task assignment
Task reassignment
Extension approval
Reassignment approval
Submission approval
Marketplace claim
Student import confirmation
```

## 82.27 Environment separation

Support:

```text
Development
Staging
Production
```

Real student data must never be copied into development seed data.

Production secrets must never exist in source control.

## 82.28 Staging deployment

Recommended deployment flow:

```text
GitHub
  ↓
CI
  ↓
Build
  ↓
Test
  ↓
Staging
  ↓
Verification
  ↓
Production
```

Frontend staging and production deployments must use separate environment variables.

## 82.29 Accessibility

Target:

```text
WCAG 2.1 AA
```

Support:

- keyboard navigation
- visible focus
- semantic HTML
- ARIA where needed
- sufficient contrast
- accessible forms
- screen-reader-friendly status messages

## 82.30 Frontend states

Every major page and data component must handle:

```text
Loading
Success
Empty
Error
Retry
Permission denied
Offline/network failure where practical
```

Do not leave blank screens for failed requests.

## 82.31 Unsaved changes

Forms such as:

```text
Task editor
Schedule editor
Availability editor
Profile editor
Settings
```

must warn users before losing unsaved changes.

## 82.32 Task visibility

Students:

```text
Own tasks → full access
Marketplace tasks → published metadata only
Other students' private tasks → no access
```

Task Managers and Admins can access department tasks according to authorization.

Reviewers can access submissions they are authorized to review.

## 82.33 Comment visibility

Define whether a comment is:

```text
STUDENT_VISIBLE
INTERNAL
```

Internal reviewer/manager notes must never accidentally be exposed to students.

If internal comments are supported, add:

```text
Visibility
```

to the Comment entity.

## 82.34 Notification preferences

Notification preferences should support per-event configuration:

```text
TaskAssigned
DeadlineReminder
Overdue
RequestResult
ReviewResult
Comment
Announcement
```

Channels:

```text
IN_APP
EMAIL
```

## 82.35 Health/readiness checks

Maintain:

```text
GET /health/live
GET /health/ready
```

Readiness should verify critical dependencies such as:

```text
PostgreSQL
Redis when enabled
Object storage when required
```

Liveness should remain lightweight.

## 82.36 Performance

Avoid N+1 queries.

Use:

- server-side projection
- pagination
- proper indexes
- query optimization
- async database operations
- cancellation tokens

Pay particular attention to:

```text
Task lists
Student workload
Analytics
Calendar
Notifications
```

## 82.37 Caching

Use caching selectively for relatively stable data such as:

```text
Skills
Categories
Settings
Active semester
```

Do not cache highly mutable authorization-sensitive data without explicit invalidation.

## 82.38 Authentication token strategy

Because frontend and backend may be hosted on different domains:

- use a secure refresh-token strategy
- prefer HttpOnly/Secure/SameSite cookies for refresh tokens where architecture permits
- keep access tokens short-lived
- rotate refresh tokens
- revoke compromised sessions
- never store long-lived secrets in localStorage

The final implementation must document the chosen browser authentication strategy and its CSRF protections.

## 82.39 CORS

Production CORS must use an explicit frontend-origin allowlist.

Development may allow the configured local frontend origin.

Never enable unrestricted production CORS.

---

# 83. FINAL PRODUCTION CHECKLIST

Before declaring the application production-ready, verify:

```text
Authentication
Invitation
Password reset
MFA preparation
Session management
Authorization
Role management
Student CRUD
Student import
Skills
Categories
Semesters
Schedule
Availability
Timezone handling

Tasks
Assignment
Assignment history
Task cancellation
Status transitions
Checklist
Dependencies
Required skills
Comments
Comment visibility
Search
Filtering
Sorting
Pagination
Optimistic concurrency

Submission
1 GB per-file limit
Multipart/direct upload
File validation
File security
File quotas
Versioning
Download authorization
Orphan cleanup

Requests
Extension
Reassignment
Concurrency
Approval/rejection

Marketplace
Publishing
Claim
Claim concurrency
Expiration
Approval/rejection

Notifications
In-app
SignalR
Email
Reminder
Retry
Idempotency
Preferences

Templates
Recurring tasks
Background jobs

Announcements
Department file storage
Folders

Analytics
Exports
Audit logs

KVKK/privacy
Data export
Retention
Deletion/anonymization
Backup
Disaster recovery

Security
Rate limiting
Brute-force protection
CORS
Secrets
Secure headers
Input validation
File security

Operations
Health checks
Readiness
Structured logging
Correlation IDs
Monitoring
Staging
CI/CD

Frontend
Loading states
Error states
Empty states
Accessibility
Responsive UI
i18n-ready architecture
Unsaved-change protection

Testing
Unit
Integration
API
Frontend
Critical flows
Concurrency
Failure scenarios
```

The result must feel like a real:

**Student Workforce & Department Operations Platform**

rather than a Todo application.

The architecture must be:

-   secure
-   modular
-   scalable
-   maintainable
-   observable
-   testable
-   deployable
-   privacy-conscious
-   ready for real users

The MVP is single-department, but the architecture must leave clean
extension points for:

``` text
Multiple departments
Multiple universities
University SSO
AI assistant
ML-based assignment
Mobile application
Advanced analytics
Multi-language support
```

Do not add unnecessary complexity merely to support these future
possibilities.

------------------------------------------------------------------------

# 80. NON-NEGOTIABLE REQUIREMENTS

All endpoint examples in this document are versioned under `/api/v1/...` in the final implementation. Older unversioned examples must be treated as documentation shorthand only.



The following must not be removed:

``` text
Invite-only authentication
Password reset
Role-based authorization
Student schedule
Availability
Workload calculation
Task assignment
Assignment history
Task checklist
Task dependencies
Required skills
Assignment recommendations
Extension requests
Reassignment requests
Submission versioning
1 GB file upload
Direct/multipart object storage upload
File validation
Orphan file cleanup
Review/revision workflow
Email reminders
In-app notifications
SignalR
Recurring tasks
Templates
Marketplace/self-assignment
Announcements
Department file storage
Analytics
Exports
Audit logs
Optimistic concurrency
KVKK/privacy controls
Backup/disaster recovery documentation
i18n-ready frontend
Docker
Vercel-compatible frontend
Container-compatible backend
PostgreSQL
Swagger/OpenAPI
Tests
CI
```

Do not simplify the requirements unless explicitly instructed.

Do not remove features silently.

Do not invent credentials.

Do not hardcode secrets.

Do not use fake APIs where a real implementation is required.

Do not expose private student information.

Do not treat frontend authorization as security.

Do not store uploaded file binaries in the database.

Do not buffer 1 GB files entirely in application memory.

Do not silently overwrite concurrent updates.

---

# 81 ADDENDUM — SEMESTER STATUS, RATE LIMITING, PRIVACY DOCS, TEST COVERAGE

This addendum closes gaps identified in the initial Section 99 skeleton.

## 81.1 Missing Enum: SemesterStatus

`Domain/Enums/` must include:

```text
SemesterStatus.cs
```

Required to support end-of-semester processing (`SemesterRolloverJob`, see Spec #29 and #56).

Suggested values:

```text
ACTIVE
ARCHIVED
```

`Semester` entity must reference this enum so the rollover job has a concrete state to transition into.

## 81.2 Missing Infrastructure Folder: Rate Limiting

`Infrastructure/` must include:

```text
Infrastructure/
└── Security/
    ├── RateLimiting/
    │   ├── RateLimitPolicy.cs
    │   ├── RateLimitStore.cs
    │   └── RateLimitOptions.cs
    ├── Audit/
    ├── DataProtection/
    └── Concurrency/
```

This is where the numeric limits defined in Spec #52 (login: 5/min/IP, forgot-password: 3/10min, etc.) are configured and enforced — not left implicit in `Api/Middleware/RateLimitingMiddleware.cs` alone. The middleware should delegate to this layer rather than hardcoding policy.

## 81.3 Compliance Documentation Split

`docs/compliance/` must contain three separate files, not one:

```text
docs/compliance/
├── KVKK.md
├── DATA_RETENTION.md
└── DATA_DELETION.md
```

`KVKK.md` covers general privacy principles (Spec #55).
`DATA_RETENTION.md` documents retention periods per entity (Spec #56).
`DATA_DELETION.md` documents deletion/anonymization procedures (Spec #57).

## 81.4 Missing Integration Test Coverage Folders

`backend/tests/StudentWorkforceManagement.IntegrationTests/` must also include:

```text
IntegrationTests/
├── Schedules/
├── Availability/
├── Feedback/
├── Announcements/
├── Analytics/
├── Exports/
└── Audit/
```

These were omitted from the initial skeleton despite corresponding controllers and application modules existing for each.

# FINAL DATA-MINIMIZATION DECISIONS

### 

`

The application must not collect, persist, import, export, index, expose, or log a university student number.

This is a deliberate data-minimization decision under KVKK. Student identity within the system is based on the application's internal immutable user/student identifier and institutional email address where required.

Do not add `StudentNumber` to:
- Student/User entities
- DTOs
- database migrations
- CSV/XLSX import
- exports
- search/filter parameters
- analytics
- audit logs
- notification payloads
- frontend forms
- API responses


# FINAL IMPLEMENTATION DECISIONS

These decisions override any older or ambiguous wording elsewhere in this document.

1. **StudentNumber**
   - Do NOT store it.
   - Do NOT import it.
   - Do NOT expose it.
   - Do NOT use it for authentication, authorization, search, analytics, or audit.

2. **Registration**
   - No public student self-registration.
   - Students are created/imported by authorized administrators and receive invitations.
   - Authentication begins through the invitation/password setup flow.

3. **Task approval**
   - REVIEWER is responsible for reviewing and approving/rejecting student submissions.
   - TASK_MANAGER may manage, assign, reassign, cancel, and administer tasks but does not approve a submission unless explicitly granted the REVIEWER permission.
   - ADMIN has all permissions.

4. **Duration unit**
   - `EstimatedDurationMinutes` is the canonical stored unit.
   - Workload calculations convert minutes to hours only for display/reporting.
   - Do not create a second duration unit in the database.

5. **Maximum individual file size**
   - 1 GB per file.
   - Total department/student storage quotas remain configurable separately.

6. **API versioning**
   - Current production API: `/api/v1/...`
   - Future breaking API: `/api/v2/...`
   - Never use `/api/v1/v2/...`.

7. **Architecture**
   - i18n-ready/key-based UI strings are required from the beginning.
   - Do not hardcode user-facing strings throughout components.
