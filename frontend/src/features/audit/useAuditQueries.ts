import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { getAuditLog, getAuditLogs } from './api/auditApi'
import type { AuditFilters } from './types'

export function useAuditLogs(filters: AuditFilters) {
  return useQuery({ queryKey: queryKeys.audit.list(stableFilters(filters)), queryFn: ({ signal }) => getAuditLogs(filters, signal) })
}

export function useAuditLog(id: string | undefined) {
  return useQuery({ queryKey: queryKeys.audit.detail(id ?? 'missing'), queryFn: ({ signal }) => getAuditLog(id ?? '', signal), enabled: Boolean(id) })
}

export function formatMetadata(value?: string) {
  if (!value) return 'None'
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
