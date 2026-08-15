# Storage Architecture

`IFileStorage` supports direct-upload contracts and provider-side metadata verification.

## Providers

- `LocalFileStorage`: development/test adapter using a configured root and application-relative upload/download contracts.
- `S3FileStorage`: S3-compatible object storage adapter using pre-signed PUT/GET URLs and metadata HEAD checks.

Storage keys are server-generated, opaque, collision-resistant values and are not authorization credentials. Business authorization must happen before creating upload or download targets.

Signed URL lifetime is configured through `Storage:SignedUrlLifetimeMinutes`; default is 15 minutes and the validation cap is 60 minutes.

## CORS

Production object storage CORS must explicitly allow only trusted frontend origins. Required methods are `PUT`, `GET`, and `HEAD`; include `POST` only if multipart upload support is enabled. Do not allow wildcard origins with credentials, and do not allow browser `DELETE` operations.

For the current production frontend, the S3-compatible bucket CORS allowlist must include `https://student-workforce-management-platfo.vercel.app` plus the approved local development origins. Backblaze B2 S3 CORS rules should permit the equivalent upload/download operations (`s3_put`, `s3_get`) and response headers needed for browser `PUT`, `GET`, and metadata verification.
