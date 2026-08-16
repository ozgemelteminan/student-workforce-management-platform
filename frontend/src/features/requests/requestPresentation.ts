import type { RequestStatus, RequestType } from './types'

export const requestTypeLabels: Record<RequestType, string> = {
  EXTENSION: 'Extension',
  REASSIGNMENT: 'Reassignment',
}

export const requestStatusLabels: Record<RequestStatus, string> = {
  PENDING: 'Pending',
  APPROVED: 'Approved',
  REJECTED: 'Rejected',
  CANCELLED: 'Cancelled',
}
