import { apiRequest } from '../../../lib/api'
import type { Semester, SemesterPayload } from '../types'

function params(filters: Record<string, string | boolean | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function getSemesters(includeInactive = false, signal?: AbortSignal) {
  return apiRequest<Semester[]>(`/semesters${params({ includeInactive })}`, { signal })
}

export function createSemester(payload: SemesterPayload) {
  return apiRequest<Semester>('/semesters', { method: 'POST', body: payload })
}

export function updateSemester(id: string, payload: SemesterPayload) {
  return apiRequest<Semester>(`/semesters/${id}`, { method: 'PUT', body: payload })
}

export function deactivateSemester(id: string) {
  return apiRequest<Semester>(`/semesters/${id}/deactivate`, { method: 'POST' })
}

export function reactivateSemester(id: string) {
  return apiRequest<Semester>(`/semesters/${id}/reactivate`, { method: 'POST' })
}
