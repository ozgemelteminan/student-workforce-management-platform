import { apiRequest } from '../../../lib/api'
import type { ReferenceDataPayload, Skill } from '../types'

export function getSkills(includeInactive = false, signal?: AbortSignal) {
  return apiRequest<Skill[]>(`/skills${includeInactive ? '?includeInactive=true' : ''}`, { signal })
}

export function createSkill(payload: ReferenceDataPayload) {
  return apiRequest<Skill>('/skills', { method: 'POST', body: payload })
}

export function updateSkill(id: string, payload: ReferenceDataPayload) {
  return apiRequest<Skill>(`/skills/${id}`, { method: 'PUT', body: payload })
}

export function deactivateSkill(id: string) {
  return apiRequest<Skill>(`/skills/${id}/deactivate`, { method: 'POST' })
}

export function reactivateSkill(id: string) {
  return apiRequest<Skill>(`/skills/${id}/reactivate`, { method: 'POST' })
}
