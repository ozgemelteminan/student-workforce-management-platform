import { apiRequest } from '../../../lib/api'
import type { Announcement, AnnouncementFilters, AnnouncementPayload, PaginatedResult } from '../types'

function params(filters: Record<string, string | number | boolean | null | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function getAnnouncements(filters: AnnouncementFilters, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<Announcement>>(`/announcements${params(filters)}`, { signal })
}

export function getAnnouncement(id: string, signal?: AbortSignal) {
  return apiRequest<Announcement>(`/announcements/${id}`, { signal })
}

export function createAnnouncement(payload: AnnouncementPayload) {
  return apiRequest<Announcement>('/announcements', { method: 'POST', body: payload })
}

export function updateAnnouncement(id: string, payload: AnnouncementPayload) {
  return apiRequest<Announcement>(`/announcements/${id}`, { method: 'PUT', body: payload })
}

export function publishAnnouncement(id: string) {
  return apiRequest<Announcement>(`/announcements/${id}/publish`, { method: 'POST' })
}

export function unpublishAnnouncement(id: string) {
  return apiRequest<Announcement>(`/announcements/${id}/unpublish`, { method: 'POST' })
}

export function pinAnnouncement(id: string) {
  return apiRequest<Announcement>(`/announcements/${id}/pin`, { method: 'POST' })
}

export function unpinAnnouncement(id: string) {
  return apiRequest<Announcement>(`/announcements/${id}/unpin`, { method: 'POST' })
}

export function deleteAnnouncement(id: string) {
  return apiRequest<void>(`/announcements/${id}`, { method: 'DELETE' })
}
