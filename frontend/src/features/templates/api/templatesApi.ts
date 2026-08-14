import { apiRequest } from '../../../lib/api'
import type { CreatedTemplateTask, CreateTaskFromTemplatePayload, PaginatedResult, TaskTemplate, TemplateFilters, TemplatePayload } from '../types'

function params(filters: Record<string, string | number | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function getTemplates(filters: TemplateFilters, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<TaskTemplate>>(`/templates${params(filters)}`, { signal })
}

export function getTemplate(id: string, signal?: AbortSignal) {
  return apiRequest<TaskTemplate>(`/templates/${id}`, { signal })
}

export function createTemplate(payload: TemplatePayload) {
  return apiRequest<TaskTemplate>('/templates', { method: 'POST', body: payload })
}

export function updateTemplate(id: string, payload: TemplatePayload) {
  return apiRequest<TaskTemplate>(`/templates/${id}`, { method: 'PUT', body: payload })
}

export function deleteTemplate(id: string) {
  return apiRequest<void>(`/templates/${id}`, { method: 'DELETE' })
}

export function createTaskFromTemplate(id: string, payload: CreateTaskFromTemplatePayload) {
  return apiRequest<CreatedTemplateTask>(`/templates/${id}/create-task`, { method: 'POST', body: payload })
}
