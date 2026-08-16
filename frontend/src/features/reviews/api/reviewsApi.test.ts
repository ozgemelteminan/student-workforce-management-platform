import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../../../lib/api'
import { getSubmissions, getTasks } from '../../tasks/api/tasksApi'
import { approveSubmission, getReviewQueue, requestSubmissionRevision } from './reviewsApi'

vi.mock('../../../lib/api', () => ({
  apiRequest: vi.fn(),
}))

vi.mock('../../tasks/api/tasksApi', () => ({
  getSubmissionVersionDownloadUrl: vi.fn(),
  getSubmissionVersions: vi.fn(),
  getSubmissions: vi.fn(),
  getTasks: vi.fn(),
  openSignedDownload: vi.fn(),
  openSignedView: vi.fn(),
}))

const mockedApiRequest = vi.mocked(apiRequest)
const mockedGetTasks = vi.mocked(getTasks)
const mockedGetSubmissions = vi.mocked(getSubmissions)

describe('reviews api client', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockedApiRequest.mockResolvedValue({})
    mockedGetTasks.mockResolvedValue({ items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false })
    mockedGetSubmissions.mockResolvedValue([])
  })

  it('requests submission revision through the canonical endpoint and body', async () => {
    await requestSubmissionRevision('submission-1', 'Please add the appendix.')

    expect(mockedApiRequest).toHaveBeenCalledTimes(1)
    expect(mockedApiRequest).toHaveBeenCalledWith('/submissions/submission-1/revision-request', {
      method: 'POST',
      body: { reviewerComment: 'Please add the appendix.' },
    })
  })

  it('approves submission through the canonical endpoint and body', async () => {
    await approveSubmission('submission-1', 'Looks good.')

    expect(mockedApiRequest).toHaveBeenCalledTimes(1)
    expect(mockedApiRequest).toHaveBeenCalledWith('/submissions/submission-1/approve', {
      method: 'POST',
      body: { reviewerComment: 'Looks good.' },
    })
  })

  it('keeps only active submitted submissions in the review queue', async () => {
    mockedGetTasks.mockResolvedValue({
      items: [{
        id: 'task-1',
        title: 'Review me',
        categoryId: 'category-1',
        priority: 'MEDIUM',
        difficulty: 'EASY',
        status: 'SUBMITTED_FOR_REVIEW',
        createdById: 'user-1',
        deadline: '2026-09-01T10:00:00Z',
        estimatedDurationMinutes: 60,
        createdAt: '2026-08-16T10:00:00Z',
        updatedAt: '2026-08-16T10:00:00Z',
        concurrencyToken: 'token-1',
      }],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    })
    mockedGetSubmissions.mockResolvedValue([
      { id: 'stale-submission', taskId: 'task-1', submittedById: 'student-1', status: 'REVISION_REQUESTED', submittedAt: '2026-08-15T10:00:00Z', concurrencyToken: 'token-2' },
      { id: 'active-submission', taskId: 'task-1', submittedById: 'student-1', status: 'SUBMITTED_FOR_REVIEW', submittedAt: '2026-08-16T10:00:00Z', concurrencyToken: 'token-3' },
    ])

    const queue = await getReviewQueue()

    expect(queue).toHaveLength(1)
    expect(queue[0]?.submissions.map((submission) => submission.id)).toEqual(['active-submission'])
  })
})
