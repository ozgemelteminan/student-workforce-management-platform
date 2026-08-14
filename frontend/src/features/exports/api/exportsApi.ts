import { buildApiUrl } from '../../../lib/api'
import { readStoredSession } from '../../../lib/auth/tokenStorage'
import { openSignedDownload } from '../../../lib/signed-urls'
import { apiRequest } from '../../../lib/api'
import type { ExportAccepted, ExportFilters, ExportPayload, ExportRequest, PaginatedResult } from '../types'

function params(filters: Record<string, string | number | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function requestExport(payload: ExportPayload) {
  return apiRequest<ExportAccepted>('/exports', { method: 'POST', body: payload })
}

export function getExports(filters: ExportFilters, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<ExportRequest>>(`/exports${params(filters)}`, { signal })
}

export function getExport(id: string, signal?: AbortSignal) {
  return apiRequest<ExportRequest>(`/exports/${id}`, { signal })
}

export async function downloadExport(id: string) {
  const token = readStoredSession()?.accessToken
  const response = await fetch(buildApiUrl(`/exports/${id}/download`), { headers: token ? { Authorization: `Bearer ${token}` } : undefined })
  if (!response.ok) {
    throw new Error('Export download is not available.')
  }
  const blob = await response.blob()
  const objectUrl = URL.createObjectURL(blob)
  openSignedDownload({ downloadUrl: objectUrl, fileName: fileNameFromDisposition(response.headers.get('content-disposition')) })
  window.setTimeout(() => URL.revokeObjectURL(objectUrl), 30_000)
}

function fileNameFromDisposition(disposition: string | null) {
  const match = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(disposition ?? '')
  const value = match?.[1] ?? match?.[2]
  return value ? decodeURIComponent(value) : undefined
}
