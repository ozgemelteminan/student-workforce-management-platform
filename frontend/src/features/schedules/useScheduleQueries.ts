import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { createAvailability, createSchedule, deleteAvailability, deleteSchedule, getActiveSemester, getCurrentStudentAvailability, getCurrentStudentSchedule, getSemesters, getStudentAvailability, getStudentSchedule, updateAvailability, updateSchedule } from './api/schedulesApi'
import type { AvailabilityPayload, SchedulePayload, UpdateAvailabilityPayload, UpdateSchedulePayload } from './types'

export function useSemesters() {
  return useQuery({ queryKey: queryKeys.semesters.all, queryFn: ({ signal }) => getSemesters(signal) })
}

export function useActiveSemester() {
  return useQuery({ queryKey: queryKeys.semesters.active(), queryFn: ({ signal }) => getActiveSemester(signal) })
}

export function useScheduleCollections(studentId: string | undefined, semesterId?: string, currentOnly = false) {
  const enabled = Boolean(studentId)
  return {
    schedule: useQuery({
      queryKey: queryKeys.schedules.student(studentId ?? 'missing', currentOnly ? 'current' : semesterId),
      queryFn: ({ signal }) => currentOnly ? getCurrentStudentSchedule(studentId ?? '', signal) : getStudentSchedule(studentId ?? '', semesterId, signal),
      enabled,
    }),
    availability: useQuery({
      queryKey: queryKeys.availability.student(studentId ?? 'missing', currentOnly ? 'current' : semesterId),
      queryFn: ({ signal }) => currentOnly ? getCurrentStudentAvailability(studentId ?? '', signal) : getStudentAvailability(studentId ?? '', semesterId, signal),
      enabled,
    }),
  }
}

export function useScheduleMutations(studentId?: string) {
  const queryClient = useQueryClient()
  const invalidate = async () => {
    if (!studentId) return
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.schedules.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.availability.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.students.detail(studentId) }),
    ])
  }

  return {
    createSchedule: useMutation({ mutationFn: (payload: SchedulePayload) => createSchedule(payload), onSuccess: async () => { appToast.success('Schedule entry saved.'); await invalidate() } }),
    updateSchedule: useMutation({ mutationFn: ({ id, payload }: { id: string; payload: UpdateSchedulePayload }) => updateSchedule(id, payload), onSuccess: async () => invalidate() }),
    deleteSchedule: useMutation({ mutationFn: (id: string) => deleteSchedule(id), onSuccess: async () => { appToast.success('Schedule entry deleted.'); await invalidate() } }),
    createAvailability: useMutation({ mutationFn: (payload: AvailabilityPayload) => createAvailability(payload), onSuccess: async () => { appToast.success('Availability saved.'); await invalidate() } }),
    updateAvailability: useMutation({ mutationFn: ({ id, payload }: { id: string; payload: UpdateAvailabilityPayload }) => updateAvailability(id, payload), onSuccess: async () => invalidate() }),
    deleteAvailability: useMutation({ mutationFn: (id: string) => deleteAvailability(id), onSuccess: async () => { appToast.success('Availability deleted.'); await invalidate() } }),
  }
}
