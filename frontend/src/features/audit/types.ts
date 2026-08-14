import type { PaginatedResult } from '../tasks/types'

export type { PaginatedResult }

export type AuditLog = {
  id: string
  userId?: string
  action: string
  entityType: string
  entityId?: string
  oldValue?: string
  newValue?: string
  ipAddress?: string
  correlationId?: string
  createdAt: string
}

export type AuditFilters = {
  page: number
  pageSize: number
  search?: string
  action?: string
  entityType?: string
  userId?: string
  entityId?: string
  from?: string
  to?: string
}
