import { apiRequest } from '../../../lib/api'
import type { AuditFilters, AuditLog, PaginatedResult } from '../types'

function params(filters: Record<string, string | number | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function getAuditLogs(filters: AuditFilters, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<AuditLog>>(`/audit${params(filters)}`, { signal })
}

export function getAuditLog(id: string, signal?: AbortSignal) {
  return apiRequest<AuditLog>(`/audit/${id}`, { signal })
}
