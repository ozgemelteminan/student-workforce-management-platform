import { apiRequest } from '../../../lib/api'
import type { Availability, AvailabilityPayload, CourseSchedule, SchedulePayload, Semester, UpdateAvailabilityPayload, UpdateSchedulePayload } from '../types'

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

export function getActiveSemester(signal?: AbortSignal) {
  return apiRequest<Semester | null>('/semesters/active', { signal })
}

export function getStudentSchedule(studentId: string, semesterId?: string, signal?: AbortSignal) {
  return apiRequest<CourseSchedule[]>(`/schedules/students/${studentId}${params({ semesterId })}`, { signal })
}

export function getCurrentStudentSchedule(studentId: string, signal?: AbortSignal) {
  return apiRequest<CourseSchedule[]>(`/schedules/students/${studentId}/current`, { signal })
}

export function createSchedule(payload: SchedulePayload) {
  return apiRequest<CourseSchedule>('/schedules', { method: 'POST', body: payload })
}

export function updateSchedule(id: string, payload: UpdateSchedulePayload) {
  return apiRequest<CourseSchedule>(`/schedules/${id}`, { method: 'PUT', body: payload })
}

export function deleteSchedule(id: string) {
  return apiRequest<void>(`/schedules/${id}`, { method: 'DELETE' })
}

export function getStudentAvailability(studentId: string, semesterId?: string, signal?: AbortSignal) {
  return apiRequest<Availability[]>(`/availability/students/${studentId}${params({ semesterId })}`, { signal })
}

export function getCurrentStudentAvailability(studentId: string, signal?: AbortSignal) {
  return apiRequest<Availability[]>(`/availability/students/${studentId}/current`, { signal })
}

export function createAvailability(payload: AvailabilityPayload) {
  return apiRequest<Availability>('/availability', { method: 'POST', body: payload })
}

export function updateAvailability(id: string, payload: UpdateAvailabilityPayload) {
  return apiRequest<Availability>(`/availability/${id}`, { method: 'PUT', body: payload })
}

export function deleteAvailability(id: string) {
  return apiRequest<void>(`/availability/${id}`, { method: 'DELETE' })
}
