import { describe, expect, it, vi } from 'vitest'
import { queryKeys } from '../../lib/query'
import { useReviewMutations } from './useReviewQueries'

const invalidateQueries = vi.fn()

vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn((options) => options),
  useQuery: vi.fn(),
  useQueryClient: () => ({ invalidateQueries }),
}))

vi.mock('../../lib/toast', () => ({
  appToast: { success: vi.fn() },
}))

vi.mock('./api/reviewsApi', () => ({
  approveSubmission: vi.fn(),
  getReviewQueue: vi.fn(),
  getVersions: vi.fn(),
  openSubmissionVersionDownload: vi.fn(),
  openSubmissionVersionView: vi.fn(),
  requestSubmissionRevision: vi.fn(),
}))

describe('useReviewMutations', () => {
  it('refreshes review and submission data after requesting a revision', async () => {
    const mutations = useReviewMutations() as unknown as {
      requestRevision: {
        onSuccess: (
          data: unknown,
          variables: { submissionId: string; reviewerComment: string; taskId?: string },
        ) => Promise<void>
      }
    }

    await mutations.requestRevision.onSuccess({}, {
      submissionId: 'submission-1',
      reviewerComment: 'Please add the appendix.',
      taskId: 'task-1',
    })

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.reviews.all })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.tasks.all })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.tasks.submissions('task-1') })
    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: queryKeys.tasks.submissionVersions('task-1', 'submission-1'),
    })
  })
})
