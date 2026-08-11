import { apiRequest } from '../../../lib/api'
import type { Notification, NotificationFilters, PaginatedResult, UnreadCountResponse } from '../types'

function params(filters: Record<string, string | number | null | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function getNotifications(filters: NotificationFilters, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<Notification>>(`/notifications${params(filters)}`, { signal })
}

export function getUnreadNotificationCount(signal?: AbortSignal) {
  return apiRequest<UnreadCountResponse>('/notifications/unread-count', { signal })
}

export function markNotificationRead(id: string) {
  return apiRequest<Notification>(`/notifications/${id}/read`, { method: 'POST' })
}

export function markAllNotificationsRead() {
  return apiRequest<UnreadCountResponse>('/notifications/read-all', { method: 'POST' })
}
