import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../../../lib/api'
import { createAvailability, deleteAvailability, updateAvailability } from './schedulesApi'
import type { AvailabilityStatus } from '../types'

vi.mock('../../../lib/api', () => ({
  apiRequest: vi.fn(),
}))

const mockedApiRequest = vi.mocked(apiRequest)

describe('schedules api client', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockedApiRequest.mockResolvedValue({})
  })

  it.each<AvailabilityStatus>(['AVAILABLE', 'UNAVAILABLE', 'PREFERRED'])('creates %s availability using the canonical DTO shape', async (status) => {
    await createAvailability({
      studentId: 'student-1',
      semesterId: 'semester-1',
      dayOfWeek: 'Monday',
      startTime: '09:00',
      endTime: '12:00',
      status,
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
        status,
        reason: 'Project work',
      },
    })
  })

  it('updates and deletes preferred availability through canonical endpoints', async () => {
    await updateAvailability('availability-1', {
      dayOfWeek: 'Tuesday',
      startTime: '13:00',
      endTime: '15:00',
      status: 'PREFERRED',
      reason: 'Good time for department work',
    })
    await deleteAvailability('availability-1')

    expect(mockedApiRequest).toHaveBeenNthCalledWith(1, '/availability/availability-1', {
      method: 'PUT',
      body: {
        dayOfWeek: 'Tuesday',
        startTime: '13:00',
        endTime: '15:00',
        status: 'PREFERRED',
        reason: 'Good time for department work',
      },
    })
    expect(mockedApiRequest).toHaveBeenNthCalledWith(2, '/availability/availability-1', { method: 'DELETE' })
  })
})
