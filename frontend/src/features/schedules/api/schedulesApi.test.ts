import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../../../lib/api'
import { createAvailability } from './schedulesApi'

vi.mock('../../../lib/api', () => ({
  apiRequest: vi.fn(),
}))

const mockedApiRequest = vi.mocked(apiRequest)

describe('schedules api client', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockedApiRequest.mockResolvedValue({})
  })

  it('creates preferred availability using the canonical DTO shape', async () => {
    await createAvailability({
      studentId: 'student-1',
      semesterId: 'semester-1',
      dayOfWeek: 'Monday',
      startTime: '09:00',
      endTime: '12:00',
      status: 'PREFERRED',
      reason: 'Project work',
    })

    expect(mockedApiRequest).toHaveBeenCalledTimes(1)
    expect(mockedApiRequest).toHaveBeenCalledWith('/availability', {
      method: 'POST',
      body: {
        studentId: 'student-1',
        semesterId: 'semester-1',
        dayOfWeek: 'Monday',
        startTime: '09:00',
        endTime: '12:00',
        status: 'PREFERRED',
        reason: 'Project work',
      },
    })
  })
})
