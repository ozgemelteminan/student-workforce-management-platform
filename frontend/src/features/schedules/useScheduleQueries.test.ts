import { describe, expect, it, vi } from 'vitest'
import { useMutation } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { useScheduleMutations } from './useScheduleQueries'

const invalidateQueries = vi.fn()

vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn((options) => options),
  useQuery: vi.fn(),
  useQueryClient: () => ({ invalidateQueries }),
}))

vi.mock('../../lib/toast', () => ({
  appToast: { success: vi.fn() },
}))

vi.mock('./api/schedulesApi', () => ({
  createAvailability: vi.fn(),
  createSchedule: vi.fn(),
  deleteAvailability: vi.fn(),
  deleteSchedule: vi.fn(),
  getActiveSemester: vi.fn(),
  getCurrentStudentAvailability: vi.fn(),
  getCurrentStudentSchedule: vi.fn(),
  getSemesters: vi.fn(),
  getStudentAvailability: vi.fn(),
  getStudentSchedule: vi.fn(),
  updateAvailability: vi.fn(),
  updateSchedule: vi.fn(),
}))

describe('useScheduleMutations', () => {
  it('refreshes timetable data after add and remove schedule mutations', async () => {
    const mutations = useScheduleMutations('student-1') as unknown as {
      createSchedule: { onSuccess: () => Promise<void> }
      createAvailability: { onSuccess: () => Promise<void> }
      deleteSchedule: { onSuccess: () => Promise<void> }
    }

    await mutations.createSchedule.onSuccess()
    await mutations.createAvailability.onSuccess()
    await mutations.deleteSchedule.onSuccess()

    expect(useMutation).toHaveBeenCalled()
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.schedules.all })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.availability.all })
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.students.detail('student-1') })
  })
})
