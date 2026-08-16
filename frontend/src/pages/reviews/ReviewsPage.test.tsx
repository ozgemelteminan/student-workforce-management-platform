import { fireEvent, render, screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ReviewsPage } from './ReviewsPage'
import type { ReviewQueueItem } from '../../features/reviews/types'

const requestRevisionMutate = vi.fn()
const approveMutate = vi.fn()
const useReviewQueue = vi.fn()
const useReviewVersions = vi.fn()

vi.mock('../../features/reviews/useReviewQueries', () => ({
  useReviewQueue: () => useReviewQueue(),
  useReviewVersions: (...args: unknown[]) => useReviewVersions(...args),
  useReviewMutations: () => ({
    approve: { mutate: approveMutate, isPending: false },
    requestRevision: { mutate: requestRevisionMutate, isPending: false },
    viewVersion: { mutate: vi.fn(), isPending: false },
    downloadVersion: { mutate: vi.fn(), isPending: false },
  }),
}))

const queueItem: ReviewQueueItem = {
  task: {
    id: 'task-1',
    title: 'Review me',
    categoryId: 'category-1',
    priority: 'MEDIUM',
    difficulty: 'EASY',
    status: 'SUBMITTED_FOR_REVIEW',
    createdById: 'user-1',
    assignedStudentId: 'student-1',
    deadline: '2026-09-01T10:00:00Z',
    estimatedDurationMinutes: 60,
    createdAt: '2026-08-16T10:00:00Z',
    updatedAt: '2026-08-16T10:00:00Z',
    concurrencyToken: 'token-1',
  },
  submissions: [
    {
      id: 'stale-submission',
      taskId: 'task-1',
      submittedById: 'student-1',
      status: 'REVISION_REQUESTED',
      submittedAt: '2026-08-15T10:00:00Z',
      concurrencyToken: 'token-2',
    },
    {
      id: 'active-submission',
      taskId: 'task-1',
      submittedById: 'student-1',
      status: 'SUBMITTED_FOR_REVIEW',
      submittedAt: '2026-08-16T10:00:00Z',
      concurrencyToken: 'token-3',
    },
  ],
}

describe('ReviewsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    useReviewQueue.mockReturnValue({ data: [queueItem], isLoading: false, isError: false, refetch: vi.fn() })
    useReviewVersions.mockReturnValue({ data: [], isLoading: false, isError: false })
  })

  it('sends one revision request for the active submitted submission and resets after success', () => {
    requestRevisionMutate.mockImplementation((_variables, options) => options?.onSuccess?.())
    render(<ReviewsPage />)

    fireEvent.click(screen.getByText('Review me'))
    fireEvent.change(screen.getByLabelText('Reviewer comment'), { target: { value: 'Please add the appendix.' } })
    fireEvent.click(screen.getByRole('button', { name: /Request revision/i }))

    expect(requestRevisionMutate).toHaveBeenCalledTimes(1)
    expect(requestRevisionMutate).toHaveBeenCalledWith(
      { submissionId: 'active-submission', reviewerComment: 'Please add the appendix.', taskId: 'task-1' },
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    )
    expect(screen.getByText('Select a submitted task to inspect versions and review the latest submission.')).toBeInTheDocument()
  })

  it('sends one approve request for the active submitted submission', () => {
    render(<ReviewsPage />)

    fireEvent.click(screen.getByText('Review me'))
    fireEvent.change(screen.getByLabelText('Reviewer comment'), { target: { value: 'Looks good.' } })
    fireEvent.click(screen.getByRole('button', { name: /^Approve$/i }))

    expect(approveMutate).toHaveBeenCalledTimes(1)
    expect(approveMutate).toHaveBeenCalledWith(
      { submissionId: 'active-submission', reviewerComment: 'Looks good.', taskId: 'task-1' },
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    )
  })

  it('keeps the reviews grid inside the shared shell content flow', () => {
    render(<ReviewsPage />)

    const grid = screen.getByTestId('reviews-layout-grid')
    expect(grid).toHaveClass('min-w-0')
    expect(grid.className).toContain('xl:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]')
    expect(within(grid).getByText('Review queue')).toBeInTheDocument()
  })

  it('renders submission version file status with a human-readable label', () => {
    useReviewVersions.mockReturnValue({
      data: [
        {
          id: 'version-1',
          taskSubmissionId: 'active-submission',
          versionNumber: 1,
          fileName: 'report.docx',
          storageKey: 'task-submissions/report.docx',
          fileSize: 2048,
          mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
          fileExtension: '.docx',
          fileStatus: 'UPLOAD_PENDING',
          uploadedById: 'student-1',
          uploadedAt: '2026-08-16T10:30:00Z',
        },
      ],
      isLoading: false,
      isError: false,
    })

    render(<ReviewsPage />)
    fireEvent.click(screen.getByText('Review me'))

    expect(screen.getByText('Upload pending')).toBeInTheDocument()
    expect(screen.queryByText('UPLOAD_PENDING')).not.toBeInTheDocument()
  })
})
