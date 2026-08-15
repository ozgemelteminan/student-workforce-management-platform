import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { approveSubmission, getReviewQueue, getVersions, openSubmissionVersionDownload, openSubmissionVersionView, requestSubmissionRevision } from './api/reviewsApi'

export function useReviewQueue() {
  return useQuery({ queryKey: queryKeys.reviews.queue(), queryFn: ({ signal }) => getReviewQueue(signal) })
}

export function useReviewVersions(taskId: string | undefined, submissionId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.reviews.versions(submissionId ?? 'missing'),
    queryFn: ({ signal }) => getVersions(taskId ?? '', submissionId ?? '', signal),
    enabled: Boolean(taskId && submissionId),
  })
}

export function useReviewMutations() {
  const queryClient = useQueryClient()
  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.reviews.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all }),
    ])
  }

  return {
    approve: useMutation({ mutationFn: ({ submissionId, reviewerComment }: { submissionId: string; reviewerComment?: string }) => approveSubmission(submissionId, reviewerComment), onSuccess: async () => { appToast.success('Submission approved.'); await invalidate() } }),
    requestRevision: useMutation({ mutationFn: ({ submissionId, reviewerComment }: { submissionId: string; reviewerComment: string }) => requestSubmissionRevision(submissionId, reviewerComment), onSuccess: async () => { appToast.success('Revision requested.'); await invalidate() } }),
    viewVersion: useMutation({ mutationFn: ({ submissionId, versionId }: { submissionId: string; versionId: string }) => openSubmissionVersionView(submissionId, versionId) }),
    downloadVersion: useMutation({ mutationFn: ({ submissionId, versionId }: { submissionId: string; versionId: string }) => openSubmissionVersionDownload(submissionId, versionId) }),
  }
}
