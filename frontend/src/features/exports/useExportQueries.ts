import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { downloadExport, getExport, getExports, requestExport } from './api/exportsApi'
import type { ExportFilters, ExportPayload, ExportStatus } from './types'

export function useExports(filters: ExportFilters) {
  return useQuery({ queryKey: queryKeys.exports.list(stableFilters(filters)), queryFn: ({ signal }) => getExports(filters, signal) })
}

export function useExport(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.exports.detail(id ?? 'missing'),
    queryFn: ({ signal }) => getExport(id ?? '', signal),
    enabled: Boolean(id),
    refetchInterval: (query) => {
      const status = query.state.data?.status
      if (isProcessingStatus(status)) return 3000
      return false
    },
  })
}

export function useExportMutations() {
  const queryClient = useQueryClient()
  return {
    create: useMutation({ mutationFn: (payload: ExportPayload) => requestExport(payload), onSuccess: async () => { appToast.success('Export requested.'); await queryClient.invalidateQueries({ queryKey: queryKeys.exports.all }) } }),
    download: useMutation({ mutationFn: (id: string) => downloadExport(id), onError: () => appToast.error('Export download is not available.') }),
  }
}

export function isProcessingStatus(status: ExportStatus | undefined) {
  return status === 'QUEUED' || status === 'PROCESSING'
}

export function isTerminalStatus(status: ExportStatus | undefined) {
  return status === 'COMPLETED' || status === 'FAILED' || status === 'EXPIRED'
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
