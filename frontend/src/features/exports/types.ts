import type { PaginatedResult } from '../tasks/types'

export type { PaginatedResult }

export type ExportType = 'Tasks' | 'Workload' | 'Students' | 'Semester' | 'PersonalData'
export type ExportFormat = 'Csv' | 'Xlsx' | 'Pdf'
export type ExportStatus = 'QUEUED' | 'PROCESSING' | 'COMPLETED' | 'FAILED' | 'EXPIRED'

export type ExportRequest = {
  id: string
  requestingUserId: string
  exportType: ExportType
  format: ExportFormat
  status: ExportStatus
  scopeId?: string
  requestedAt: string
  processingStartedAt?: string
  completedAt?: string
  failedAt?: string
  expiresAt?: string
  failureReason?: string
  artifactFileName?: string
  artifactFileSize?: number
  artifactMimeType?: string
  concurrencyToken: string
}

export type ExportAccepted = {
  id: string
  status: ExportStatus
  statusUrl: string
}

export type ExportFilters = {
  page: number
  pageSize: number
  search?: string
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export type ExportPayload = {
  type: ExportType
  format: ExportFormat
  scopeId?: string
}
