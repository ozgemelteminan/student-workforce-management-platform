import { apiRequest } from '../../../lib/api'
import type { SystemSetting } from '../types'

export function getSettings(signal?: AbortSignal) {
  return apiRequest<SystemSetting[]>('/settings', { signal })
}

export function updateSetting(key: string, value: string, concurrencyToken: string) {
  return apiRequest<SystemSetting>(`/settings/${encodeURIComponent(key)}`, { method: 'PUT', body: { value, concurrencyToken } })
}
