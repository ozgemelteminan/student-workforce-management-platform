import type { PaginatedResult } from '../tasks/types'

export type { PaginatedResult }

export type RequestType = 'EXTENSION' | 'REASSIGNMENT'
export type RequestStatus = 'PENDING' | 'APPROVED' | 'REJECTED' | 'CANCELLED'

export type TaskRequest = {
  id: string
  taskId: string
  requestedById: string
  type: RequestType
  reason: string
  currentDeadline?: string
  requestedDeadline?: string
  suggestedStudentId?: string
  status: RequestStatus
  createdAt: string
  reviewedAt?: string
  reviewedById?: string
  reviewerComment?: string
  concurrencyToken: string
}

export type RequestFilters = {
  page: number
  pageSize: number
  search?: string
  taskId?: string
  type?: RequestType
  status?: RequestStatus
}

export type ExtensionRequestPayload = {
  taskId: string
  requestedDeadline: string
  reason: string
}

export type ReassignmentRequestPayload = {
  taskId: string
  reason: string
  suggestedStudentId?: string
}
