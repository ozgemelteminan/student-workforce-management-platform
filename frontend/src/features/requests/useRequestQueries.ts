import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { approveRequest, cancelRequest, createExtensionRequest, createReassignmentRequest, getRequests, rejectRequest } from './api/requestsApi'
import type { ExtensionRequestPayload, ReassignmentRequestPayload, RequestFilters } from './types'

export function useRequests(filters: RequestFilters) {
  return useQuery({ queryKey: queryKeys.requests.list(stableFilters(filters)), queryFn: ({ signal }) => getRequests(filters, signal) })
}

export function useRequestMutations() {
  const queryClient = useQueryClient()
  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.requests.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all }),
    ])
  }

  return {
    createExtension: useMutation({ mutationFn: (payload: ExtensionRequestPayload) => createExtensionRequest(payload), onSuccess: async () => { appToast.success('Extension request submitted.'); await invalidate() } }),
    createReassignment: useMutation({ mutationFn: (payload: ReassignmentRequestPayload) => createReassignmentRequest(payload), onSuccess: async () => { appToast.success('Reassignment request submitted.'); await invalidate() } }),
    approve: useMutation({ mutationFn: ({ id, reviewerComment, newAssigneeId }: { id: string; reviewerComment?: string; newAssigneeId?: string }) => approveRequest(id, reviewerComment, newAssigneeId), onSuccess: async () => { appToast.success('Request approved.'); await invalidate() } }),
    reject: useMutation({ mutationFn: ({ id, reviewerComment }: { id: string; reviewerComment: string }) => rejectRequest(id, reviewerComment), onSuccess: async () => { appToast.success('Request rejected.'); await invalidate() } }),
    cancel: useMutation({ mutationFn: (id: string) => cancelRequest(id), onSuccess: async () => { appToast.success('Request cancelled.'); await invalidate() } }),
  }
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
