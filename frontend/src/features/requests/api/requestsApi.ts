import { apiRequest } from '../../../lib/api'
import type { ExtensionRequestPayload, PaginatedResult, ReassignmentRequestPayload, RequestFilters, TaskRequest } from '../types'

function params(filters: Record<string, string | number | null | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function getRequests(filters: RequestFilters, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<TaskRequest>>(`/requests${params(filters)}`, { signal })
}

export function createExtensionRequest(payload: ExtensionRequestPayload) {
  return apiRequest<TaskRequest>('/requests/extension', { method: 'POST', body: payload })
}

export function createReassignmentRequest(payload: ReassignmentRequestPayload) {
  return apiRequest<TaskRequest>('/requests/reassignment', { method: 'POST', body: payload })
}

export function approveRequest(id: string, reviewerComment?: string, newAssigneeId?: string) {
  return apiRequest<TaskRequest>(`/requests/${id}/approve`, { method: 'POST', body: { reviewerComment, newAssigneeId } })
}

export function rejectRequest(id: string, reviewerComment: string) {
  return apiRequest<TaskRequest>(`/requests/${id}/reject`, { method: 'POST', body: { reviewerComment } })
}

export function cancelRequest(id: string) {
  return apiRequest<TaskRequest>(`/requests/${id}/cancel`, { method: 'POST' })
}
