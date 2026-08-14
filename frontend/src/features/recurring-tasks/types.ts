import type { PaginatedResult } from '../tasks/types'

export type { PaginatedResult }

export type RecurringTask = {
  id: string
  templateId: string
  frequency: string
  timeZoneId: string
  localRunTime?: string
  nextRunAt: string
  isActive: boolean
  createdById: string
  concurrencyToken: string
  createdAt: string
  updatedAt?: string
}

export type RecurringTaskFilters = {
  page: number
  pageSize: number
  search?: string
  isActive?: boolean
}

export type RecurringTaskPayload = {
  templateId?: string
  frequency: string
  timeZoneId: string
  localRunTime?: string
  nextRunAt: string
}
