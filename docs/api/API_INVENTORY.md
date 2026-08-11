# API Inventory

All application endpoints use `/api/v1/...` unless a future breaking version is explicitly introduced as `/api/v2/...`. Infrastructure and support routes are reported separately and are not included in the application endpoint count.

## Summary

- Mapped application API endpoints: 139
- Functional application API endpoints: 139
- Deferred functional API endpoints: 0
- Implemented but undocumented endpoints: 0
- Documented but missing endpoints: 0

## Application API Endpoints

### Root

- `GET /api/v1`

### Auth

- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/reset-password`
- `POST /api/v1/invitations/accept`

### Account

- `GET /api/v1/invitations`
- `POST /api/v1/invitations`
- `POST /api/v1/invitations/{id}/resend`
- `POST /api/v1/invitations/{id}/revoke`
- `GET /api/v1/sessions`
- `DELETE /api/v1/sessions`
- `DELETE /api/v1/sessions/{sessionId}`

### Catalog

- `GET /api/v1/categories`
- `POST /api/v1/categories`
- `GET /api/v1/skills`
- `POST /api/v1/skills`
- `POST /api/v1/students/{studentId}/skills`
- `GET /api/v1/tasks/{id}/skills`

### Students

- `GET /api/v1/students`
- `GET /api/v1/students/me`
- `GET /api/v1/students/{id}`
- `PUT /api/v1/students/{id}`
- `POST /api/v1/students/{id}/activate`
- `POST /api/v1/students/{id}/deactivate`

### Tasks

- `GET /api/v1/tasks`
- `POST /api/v1/tasks`
- `GET /api/v1/tasks/my`
- `GET /api/v1/tasks/{id}`
- `PUT /api/v1/tasks/{id}`
- `POST /api/v1/tasks/{id}/accept`
- `POST /api/v1/tasks/{id}/assign`
- `POST /api/v1/tasks/{id}/cancel`
- `GET /api/v1/tasks/{id}/checklist`
- `POST /api/v1/tasks/{id}/checklist`
- `POST /api/v1/tasks/{id}/checklist/{itemId}/complete`
- `POST /api/v1/tasks/{id}/checklist/{itemId}/uncomplete`
- `GET /api/v1/tasks/{id}/comments`
- `POST /api/v1/tasks/{id}/comments`
- `PUT /api/v1/tasks/{id}/comments/{commentId}`
- `DELETE /api/v1/tasks/{id}/comments/{commentId}`
- `GET /api/v1/tasks/{id}/dependencies`
- `POST /api/v1/tasks/{id}/dependencies`
- `GET /api/v1/tasks/{id}/history`
- `POST /api/v1/tasks/{id}/reassign`
- `GET /api/v1/tasks/{id}/recommendations`
- `POST /api/v1/tasks/{id}/start`
- `POST /api/v1/tasks/{id}/submit`
- `POST /api/v1/tasks/{id}/unassign`

### Requests

- `GET /api/v1/requests`
- `POST /api/v1/requests/extension`
- `POST /api/v1/requests/reassignment`
- `POST /api/v1/requests/{id}/approve`
- `POST /api/v1/requests/{id}/cancel`
- `POST /api/v1/requests/{id}/reject`

### Submissions

- `GET /api/v1/tasks/{taskId}/submissions`
- `POST /api/v1/tasks/{taskId}/submissions/uploads`
- `GET /api/v1/submissions/{id}/versions`
- `POST /api/v1/submissions/versions/{versionId}/complete`
- `POST /api/v1/submissions/{id}/approve`
- `POST /api/v1/submissions/{id}/revision-request`

### Marketplace

- `GET /api/v1/marketplace/listings`
- `POST /api/v1/marketplace/tasks/{taskId}/publish`
- `POST /api/v1/marketplace/listings/{id}/unpublish`
- `POST /api/v1/marketplace/listings/{id}/claims`
- `POST /api/v1/marketplace/claims/{id}/approve`
- `POST /api/v1/marketplace/claims/{id}/cancel`
- `POST /api/v1/marketplace/claims/{id}/reject`

### Scheduling

- `GET /api/v1/semesters`
- `POST /api/v1/semesters`
- `GET /api/v1/semesters/active`
- `GET /api/v1/semesters/{id}`
- `PUT /api/v1/semesters/{id}`
- `DELETE /api/v1/semesters/{id}`
- `POST /api/v1/semesters/{id}/activate`
- `POST /api/v1/semesters/{id}/archive`
- `GET /api/v1/schedules/students/{studentId}`
- `GET /api/v1/schedules/students/{studentId}/current`
- `POST /api/v1/schedules`
- `PUT /api/v1/schedules/{id}`
- `DELETE /api/v1/schedules/{id}`
- `GET /api/v1/availability/students/{studentId}`
- `GET /api/v1/availability/students/{studentId}/current`
- `POST /api/v1/availability`
- `PUT /api/v1/availability/{id}`
- `DELETE /api/v1/availability/{id}`

### Files

- `GET /api/v1/files`
- `POST /api/v1/files/uploads`
- `POST /api/v1/files/{id}/complete`
- `GET /api/v1/files/{id}/download`
- `DELETE /api/v1/files/{id}`
- `GET /api/v1/files/folders`
- `POST /api/v1/files/folders`
- `PUT /api/v1/files/folders/{id}`
- `DELETE /api/v1/files/folders/{id}`

### Announcements

- `GET /api/v1/announcements`
- `POST /api/v1/announcements`
- `GET /api/v1/announcements/{id}`
- `PUT /api/v1/announcements/{id}`
- `DELETE /api/v1/announcements/{id}`
- `POST /api/v1/announcements/{id}/pin`
- `POST /api/v1/announcements/{id}/publish`
- `POST /api/v1/announcements/{id}/unpin`
- `POST /api/v1/announcements/{id}/unpublish`

### Notifications

- `GET /api/v1/notifications`
- `GET /api/v1/notifications/unread-count`
- `POST /api/v1/notifications/{id}/read`
- `POST /api/v1/notifications/read-all`
- `PUT /api/v1/notifications/preferences`

### Feedback

- `GET /api/v1/tasks/{taskId}/feedback`
- `POST /api/v1/tasks/{taskId}/feedback`
- `GET /api/v1/students/{studentId}/feedback`

### Templates

- `GET /api/v1/templates`
- `POST /api/v1/templates`
- `GET /api/v1/templates/{id}`
- `POST /api/v1/templates/{id}/create-task`
- `PUT /api/v1/templates/{id}`
- `DELETE /api/v1/templates/{id}`

### Recurring Tasks

- `GET /api/v1/recurring-tasks`
- `POST /api/v1/recurring-tasks`
- `GET /api/v1/recurring-tasks/{id}`
- `PUT /api/v1/recurring-tasks/{id}`
- `DELETE /api/v1/recurring-tasks/{id}`
- `POST /api/v1/recurring-tasks/{id}/activate`
- `POST /api/v1/recurring-tasks/{id}/deactivate`

### Analytics

- `GET /api/v1/analytics/dashboard`
- `GET /api/v1/analytics/requests`
- `GET /api/v1/analytics/tasks/category`
- `GET /api/v1/analytics/tasks/status`
- `GET /api/v1/analytics/workload`

### Settings

- `GET /api/v1/settings`
- `PUT /api/v1/settings/{key}`

### Audit

- `GET /api/v1/audit`
- `GET /api/v1/audit/{id}`

### Exports

- `POST /api/v1/exports`
- `GET /api/v1/exports`
- `GET /api/v1/exports/{id}`
- `GET /api/v1/exports/{id}/download`

## Infrastructure / Support Surfaces

- `GET /health`
- `GET /health/live`
- `GET /health/ready`
- SignalR hub: `/hubs/notifications`
- Hangfire dashboard: `/admin/hangfire`
- Runtime OpenAPI document in Development: `/openapi/v1.json`

## Export Lifecycle

Exports are implemented as a durable asynchronous lifecycle:

Create persists a queued request and returns `202 Accepted`; list returns visible export requests; detail/status blocks IDOR by visibility filtering; download returns an authorized expiring contract only for completed, non-expired artifacts.

No export lifecycle endpoint is intentionally deferred.
