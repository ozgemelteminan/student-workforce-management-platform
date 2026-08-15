import { apiRequest } from '../../../lib/api'
import type { Category, ReferenceDataPayload } from '../types'

export function getCategories(includeInactive = false, signal?: AbortSignal) {
  return apiRequest<Category[]>(`/categories${includeInactive ? '?includeInactive=true' : ''}`, { signal })
}

export function createCategory(payload: ReferenceDataPayload) {
  return apiRequest<Category>('/categories', { method: 'POST', body: payload })
}

export function updateCategory(id: string, payload: ReferenceDataPayload) {
  return apiRequest<Category>(`/categories/${id}`, { method: 'PUT', body: payload })
}

export function deactivateCategory(id: string) {
  return apiRequest<Category>(`/categories/${id}/deactivate`, { method: 'POST' })
}

export function reactivateCategory(id: string) {
  return apiRequest<Category>(`/categories/${id}/reactivate`, { method: 'POST' })
}
