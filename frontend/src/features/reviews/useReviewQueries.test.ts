import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../lib/api'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { useReviewMutations } from './useReviewQueries'

const invalidateQueries = vi.fn()

vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn((options) => options),
  useQuery: vi.fn(),
  useQueryClient: () => ({ invalidateQueries }),
}))

vi.mock('../../lib/toast', () => ({
  appToast: { success: vi.fn(), error: vi.fn() },
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
  it('refreshes review and submission data after approving a submission', async () => {
    const mutations = useReviewMutations() as unknown as {
      approve: {
        onSuccess: (
          data: unknown,
          variables: { submissionId: string; reviewerComment?: string; taskId?: string },
        ) => Promise<void>
      }
    }

    await mutations.approve.onSuccess({}, {
      submissionId: 'submission-1',
      reviewerComment: 'Looks good.',
      taskId: 'task-1',
    })

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.reviews.all })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.tasks.all })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.tasks.submissions('task-1') })
    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: queryKeys.tasks.submissionVersions('task-1', 'submission-1'),
    })
  })

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

  it('shows an error toast when a review mutation fails', () => {
    const mutations = useReviewMutations() as unknown as {
      requestRevision: {
        onError: (error: unknown) => void
      }
    }

    mutations.requestRevision.onError(new ApiError({
      status: 409,
      title: 'Conflict',
      detail: 'Only submitted submissions can be reviewed.',
      validationErrors: {},
    }))

    expect(appToast.error).toHaveBeenCalledWith('Only submitted submissions can be reviewed.')
  })
})
