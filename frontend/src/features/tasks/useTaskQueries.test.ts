import { describe, expect, it, vi } from 'vitest'
import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { useTaskLookups } from './useTaskQueries'

vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn((options) => options),
  useQuery: vi.fn((options) => options),
  useQueryClient: () => ({ invalidateQueries: vi.fn() }),
}))

vi.mock('../../lib/toast', () => ({
  appToast: { success: vi.fn() },
}))

describe('useTaskLookups', () => {
  it('does not enable the staff-only student directory query for student-only views', () => {
    useTaskLookups({ studentsEnabled: false })

    expect(useQuery).toHaveBeenCalledWith(expect.objectContaining({
      queryKey: queryKeys.students.list({ page: 1, pageSize: 50 }),
      enabled: false,
    }))
  })

  it('keeps the student directory query enabled for staff workflows', () => {
    vi.clearAllMocks()

    useTaskLookups({ studentsEnabled: true })

    expect(useQuery).toHaveBeenCalledWith(expect.objectContaining({
      queryKey: queryKeys.students.list({ page: 1, pageSize: 50 }),
      enabled: true,
    }))
  })
})
