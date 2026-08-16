import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../../../lib/api'
import { requestSubmissionRevision } from './reviewsApi'

vi.mock('../../../lib/api', () => ({
  apiRequest: vi.fn(),
}))

const mockedApiRequest = vi.mocked(apiRequest)

describe('reviews api client', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockedApiRequest.mockResolvedValue({})
  })

  it('requests submission revision through the canonical endpoint and body', async () => {
    await requestSubmissionRevision('submission-1', 'Please add the appendix.')

    expect(mockedApiRequest).toHaveBeenCalledTimes(1)
    expect(mockedApiRequest).toHaveBeenCalledWith('/submissions/submission-1/revision-request', {
      method: 'POST',
      body: { reviewerComment: 'Please add the appendix.' },
    })
  })
})
