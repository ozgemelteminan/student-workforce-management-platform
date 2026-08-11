import { Badge } from './badge'

export type WorkflowStatus =
  | 'ASSIGNED'
  | 'ACCEPTED'
  | 'IN_PROGRESS'
  | 'SUBMITTED'
  | 'SUBMITTED_FOR_REVIEW'
  | 'REVISION_REQUESTED'
  | 'COMPLETED'
  | 'APPROVED'
  | 'CANCELLED'
  | 'OVERDUE'

const statusStyles: Record<WorkflowStatus, { label: string; variant: 'neutral' | 'success' | 'warning' | 'info' | 'danger' }> = {
  ASSIGNED: { label: 'Assigned', variant: 'neutral' },
  ACCEPTED: { label: 'Accepted', variant: 'info' },
  IN_PROGRESS: { label: 'In progress', variant: 'info' },
  SUBMITTED: { label: 'Submitted', variant: 'info' },
  SUBMITTED_FOR_REVIEW: { label: 'Submitted for review', variant: 'info' },
  REVISION_REQUESTED: { label: 'Revision requested', variant: 'warning' },
  COMPLETED: { label: 'Completed', variant: 'success' },
  APPROVED: { label: 'Approved', variant: 'success' },
  CANCELLED: { label: 'Cancelled', variant: 'neutral' },
  OVERDUE: { label: 'Overdue', variant: 'danger' },
}

export function StatusBadge({ status }: { status: WorkflowStatus }) {
  const meta = statusStyles[status]
  return (
    <Badge variant={meta.variant}>
      <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-current" />
      {meta.label}
    </Badge>
  )
}
