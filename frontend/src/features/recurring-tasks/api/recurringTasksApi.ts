import { apiRequest } from '../../../lib/api'
import type { PaginatedResult, RecurringTask, RecurringTaskFilters, RecurringTaskPayload } from '../types'

function params(filters: Record<string, string | number | boolean | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function getRecurringTasks(filters: RecurringTaskFilters, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<RecurringTask>>(`/recurring-tasks${params(filters)}`, { signal })
}

export function getRecurringTask(id: string, signal?: AbortSignal) {
  return apiRequest<RecurringTask>(`/recurring-tasks/${id}`, { signal })
}

export function createRecurringTask(payload: Required<RecurringTaskPayload>) {
  return apiRequest<RecurringTask>('/recurring-tasks', { method: 'POST', body: payload })
}

export function updateRecurringTask(id: string, payload: Omit<RecurringTaskPayload, 'templateId'>) {
  return apiRequest<RecurringTask>(`/recurring-tasks/${id}`, { method: 'PUT', body: payload })
}

export function activateRecurringTask(id: string) {
  return apiRequest<RecurringTask>(`/recurring-tasks/${id}/activate`, { method: 'POST' })
}

export function deactivateRecurringTask(id: string) {
  return apiRequest<RecurringTask>(`/recurring-tasks/${id}/deactivate`, { method: 'POST' })
}

export function deleteRecurringTask(id: string) {
  return apiRequest<void>(`/recurring-tasks/${id}`, { method: 'DELETE' })
}
