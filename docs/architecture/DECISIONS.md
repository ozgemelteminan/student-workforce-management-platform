# Architecture Decisions

## Canonical decisions applied in foundation

- Student identity excludes university student numbers entirely.
- `TaskAssignmentHistory` is the canonical assignment history model.
- `TaskRequest` is the canonical persistence model for extension and reassignment requests.
- `SubmissionVersion` is the canonical submission version model.
- REVIEWER has submission approval authority; TASK_MANAGER does not by default.
- Login rate limit: 5 failed attempts within 15 minutes by IP and account.
- Forgot-password rate limit: 3 requests per hour by account and IP.
- API version foundation: `/api/v1`.
- Persisted timestamps are UTC; display timezone starts as Europe/Istanbul.
- File architecture must support direct/multipart object storage uploads up to 1 GB without full-file buffering.
