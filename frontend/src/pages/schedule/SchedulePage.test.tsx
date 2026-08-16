import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../lib/api'
import { SchedulePage } from './SchedulePage'

const useStudents = vi.fn()
const useCurrentStudent = vi.fn()
const useSemesters = vi.fn()
const useActiveSemester = vi.fn()
const useScheduleCollections = vi.fn()
const useScheduleMutations = vi.fn()
const authState = { roles: ['STUDENT'] as string[] }

vi.mock('../../lib/auth/AuthProvider', () => ({
  useAuth: () => ({ user: authState }),
}))

vi.mock('../../features/students/useStudentQueries', () => ({
  useStudents: (...args: unknown[]) => useStudents(...args),
  useCurrentStudent: (...args: unknown[]) => useCurrentStudent(...args),
}))

vi.mock('../../features/schedules/useScheduleQueries', () => ({
  useSemesters: (...args: unknown[]) => useSemesters(...args),
  useActiveSemester: (...args: unknown[]) => useActiveSemester(...args),
  useScheduleCollections: (...args: unknown[]) => useScheduleCollections(...args),
  useScheduleMutations: (...args: unknown[]) => useScheduleMutations(...args),
}))

const createAvailability = vi.fn()

describe('SchedulePage availability workflow', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    createAvailability.mockResolvedValue({})
    authState.roles = ['STUDENT']
    useStudents.mockReturnValue({ data: { items: [] } })
    useCurrentStudent.mockReturnValue({ data: { student: { id: 'student-1', firstName: 'Test', lastName: 'Student' } } })
    useSemesters.mockReturnValue({ data: [{ id: 'semester-1', name: 'Fall 2026', status: 'ACTIVE' }] })
    useActiveSemester.mockReturnValue({ data: { id: 'semester-1', name: 'Fall 2026', startDate: '2026-09-01', endDate: '2027-01-15' } })
    useScheduleCollections.mockReturnValue({
      schedule: { data: [], isLoading: false, isError: false, refetch: vi.fn() },
      availability: { data: [], isLoading: false, isError: false, refetch: vi.fn() },
    })
    useScheduleMutations.mockReturnValue({
      createSchedule: { mutate: vi.fn(), isPending: false },
      updateSchedule: { mutate: vi.fn(), isPending: false },
      deleteSchedule: { mutate: vi.fn(), isPending: false },
      createAvailability: { mutateAsync: createAvailability, isPending: false },
      updateAvailability: { mutateAsync: vi.fn(), isPending: false },
      deleteAvailability: { mutate: vi.fn(), isPending: false },
    })
  })

  it('does not enable the staff-only student directory for student users', () => {
    render(<SchedulePage />)

    expect(useStudents).toHaveBeenCalledWith({ page: 1, pageSize: 100, sortBy: 'name', sortDirection: 'asc' }, false)
    expect(screen.queryByText('MondayMondayTuesdayWednesdayThursdayFridaySaturdaySunday')).not.toBeInTheDocument()
    expect(screen.queryByText('AVAILABLEAVAILABLEUNAVAILABLEPREFERRED')).not.toBeInTheDocument()
  })

  it('shows only the select-student prompt before staff chooses a student', () => {
    authState.roles = ['ADMIN']
    useCurrentStudent.mockReturnValue({ data: undefined })
    useStudents.mockReturnValue({ data: { items: [{ id: 'student-2', firstName: 'Ada', lastName: 'Lovelace' }] } })

    render(<SchedulePage />)

    expect(screen.getByText('Select a student to view schedule and availability.')).toBeInTheDocument()
    expect(screen.queryByText('No course schedule entries.')).not.toBeInTheDocument()
    expect(screen.queryByText('No availability entries.')).not.toBeInTheDocument()
  })

  it('shows local validation for invalid availability time ranges', () => {
    render(<SchedulePage />)

    fireEvent.change(screen.getAllByLabelText('End')[1]!, { target: { value: '08:00' } })

    expect(screen.getAllByText('End time must be after start time.')).toHaveLength(2)
    expect(screen.getByRole('button', { name: /Add availability/i })).toBeDisabled()
  })

  it('displays a clear conflict message for real availability overlaps', async () => {
    createAvailability.mockRejectedValue(new ApiError({
      status: 409,
      title: 'Conflict',
      detail: 'Availability overlaps an existing availability record.',
      validationErrors: {},
    }))
    render(<SchedulePage />)

    fireEvent.click(screen.getByRole('button', { name: /Add availability/i }))

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Availability overlaps an existing availability record.'))
    expect(createAvailability).toHaveBeenCalledTimes(1)
  })

  it('displays validation messages returned by availability create', async () => {
    createAvailability.mockRejectedValue(new ApiError({
      status: 400,
      title: 'Validation failed',
      validationErrors: { EndTime: ['Availability end time must be after start time.'] },
    }))
    render(<SchedulePage />)

    fireEvent.click(screen.getByRole('button', { name: /Add availability/i }))

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Availability end time must be after start time.'))
    expect(createAvailability).toHaveBeenCalledTimes(1)
  })
})
