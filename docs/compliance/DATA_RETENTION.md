# Data Retention

Entity-specific retention periods must be defined before production readiness.

| Data category | Defined retention behavior | Automated action | Preservation requirement |
| --- | --- | --- | --- |
| Student | Not specified | Manual review required | Preserve history where legally required |
| Task | Not specified | Manual review required | Preserve assignment and task history |
| Comment | Not specified | Manual review required | Preserve task context where required |
| Submission | Not specified | Manual review required | Do not destroy immutable submission history without explicit policy |
| DepartmentFile | Pending upload grace cleanup only | Mark stale pending uploads failed | Do not delete files still referenced by immutable records |
| Announcement | Not specified | Manual review required | Preserve audit trail |
| Notification | Not specified | Manual review required | Preserve user history until policy exists |
| AuditLog | Not specified | Must preserve | Audit logs are immutable unless an explicit legal retention policy says otherwise |

No hard-delete or anonymization automation is enabled until entity-specific eligibility, cutoff periods, and legal/audit preservation rules are approved.
