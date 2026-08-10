# Test Plan

Backend unit, integration, and architecture test projects exist. Frontend Vitest, Testing Library, fixtures, unit, component, and integration locations exist.


## Infrastructure Provider Tests

Current automated coverage includes Identity password hashing, secure token generation/hash lookup, local storage path traversal protection, durable email queue/dispatch idempotency, and overdue-job idempotency.

Testcontainers packages are installed for PostgreSQL and Redis. Container-backed tests should be run in an environment with Docker available; report `NOT EXECUTED -- Docker unavailable` when Docker is not running.

## Infrastructure Gap Closure Tests

Coverage includes persisted Data Protection key-ring restart compatibility, wrong-key-ring email dispatch failure behavior, protected email secret cleanup after send, recurring local-time next occurrence calculation, monthly end-of-month clamping, recurring occurrence duplicate prevention, and unsupported recurring frequency failure recording.
