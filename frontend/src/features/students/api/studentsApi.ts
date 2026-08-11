import { apiRequest } from '../../../lib/api'
import type { Feedback, PaginatedResult, Skill, Student, StudentFilters, StudentProfile, StudentSkillDetail, UpsertStudentSkillPayload, UpdateStudentPayload } from '../types'

function params(filters: Record<string, string | number | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function getStudents(filters: StudentFilters, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<Student>>(`/students${params(filters)}`, { signal })
}

export function getStudent(id: string, signal?: AbortSignal) {
  return apiRequest<StudentProfile>(`/students/${id}`, { signal })
}

export function getCurrentStudent(signal?: AbortSignal) {
  return apiRequest<StudentProfile>('/students/me', { signal })
}

export function updateStudent(id: string, payload: UpdateStudentPayload) {
  return apiRequest<Student>(`/students/${id}`, { method: 'PUT', body: payload })
}

export function activateStudent(id: string) {
  return apiRequest<Student>(`/students/${id}/activate`, { method: 'POST' })
}

export function deactivateStudent(id: string) {
  return apiRequest<Student>(`/students/${id}/deactivate`, { method: 'POST' })
}

export function getStudentFeedback(studentId: string, page = 1, pageSize = 10, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<Feedback>>(`/students/${studentId}/feedback${params({ page, pageSize })}`, { signal })
}

export function getStudentSkills(studentId: string, signal?: AbortSignal) {
  return apiRequest<StudentSkillDetail[]>(`/students/${studentId}/skills`, { signal })
}

export function getSkills(signal?: AbortSignal) {
  return apiRequest<Skill[]>('/skills', { signal })
}

export function upsertStudentSkill(studentId: string, payload: UpsertStudentSkillPayload) {
  return apiRequest(`/students/${studentId}/skills`, { method: 'POST', body: payload })
}
