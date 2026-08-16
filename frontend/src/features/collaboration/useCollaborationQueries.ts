import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { addMeetingActionItem, cancelMeeting, confirmMeeting, convertActionItemToTask, createMeeting, createUnavailability, deleteTimesheetEntry, deleteUnavailability, getCurrentTimesheet, getMeeting, getMeetings, getMeetingSlots, getTimesheets, getUnavailability, respondToMeeting, reviewTimesheet, submitTimesheet, updateMeetingNotes, upsertTimesheetEntry } from './api/collaborationApi'
import type { ActionItemTaskPayload, CreateMeetingPayload, MeetingStatus, TimesheetStatus } from './types'

export function useCurrentTimesheet() {
  return useQuery({ queryKey: queryKeys.timesheets.current(), queryFn: ({ signal }) => getCurrentTimesheet(signal) })
}

export function useTimesheets(filters: { page: number; pageSize: number; studentId?: string; status?: TimesheetStatus }) {
  return useQuery({ queryKey: queryKeys.timesheets.list(stableFilters(filters)), queryFn: ({ signal }) => getTimesheets(filters, signal) })
}

export function useUnavailability(filters: { studentId?: string } = {}) {
  return useQuery({ queryKey: queryKeys.unavailability.list(stableFilters(filters)), queryFn: ({ signal }) => getUnavailability(filters, signal) })
}

export function useMeetings(filters: { page: number; pageSize: number; search?: string; status?: MeetingStatus }) {
  return useQuery({ queryKey: queryKeys.meetings.list(stableFilters(filters)), queryFn: ({ signal }) => getMeetings(filters, signal) })
}

export function useMeeting(id: string | undefined) {
  return useQuery({ queryKey: queryKeys.meetings.detail(id ?? 'missing'), queryFn: ({ signal }) => getMeeting(id ?? '', signal), enabled: Boolean(id) })
}

export function useMeetingSlots(id: string | undefined, enabled = true) {
  return useQuery({ queryKey: queryKeys.meetings.slots(id ?? 'missing'), queryFn: ({ signal }) => getMeetingSlots(id ?? '', signal), enabled: Boolean(id && enabled) })
}

export function useCollaborationMutations() {
  const queryClient = useQueryClient()
  const invalidateTimesheets = async () => queryClient.invalidateQueries({ queryKey: queryKeys.timesheets.all })
  const invalidateMeetings = async (id?: string) => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.meetings.all }),
      id ? queryClient.invalidateQueries({ queryKey: queryKeys.meetings.detail(id) }) : Promise.resolve(),
    ])
  }
  return {
    upsertEntry: useMutation({ mutationFn: upsertTimesheetEntry, onSuccess: async () => { appToast.success('Time entry saved.'); await invalidateTimesheets() } }),
    deleteEntry: useMutation({ mutationFn: deleteTimesheetEntry, onSuccess: async () => { appToast.success('Time entry deleted.'); await invalidateTimesheets() } }),
    submitWeek: useMutation({ mutationFn: submitTimesheet, onSuccess: async () => { appToast.success('Timesheet submitted.'); await invalidateTimesheets() } }),
    reviewWeek: useMutation({ mutationFn: ({ id, status, reviewerComment }: { id: string; status: Extract<TimesheetStatus, 'APPROVED' | 'NEEDS_CORRECTION'>; reviewerComment?: string }) => reviewTimesheet(id, status, reviewerComment), onSuccess: async () => invalidateTimesheets() }),
    createUnavailability: useMutation({ mutationFn: createUnavailability, onSuccess: async () => { appToast.success('Unavailability saved.'); await queryClient.invalidateQueries({ queryKey: queryKeys.unavailability.all }) } }),
    deleteUnavailability: useMutation({ mutationFn: deleteUnavailability, onSuccess: async () => queryClient.invalidateQueries({ queryKey: queryKeys.unavailability.all }) }),
    createMeeting: useMutation({ mutationFn: (payload: CreateMeetingPayload) => createMeeting(payload), onSuccess: async (meeting) => { appToast.success('Meeting created.'); await invalidateMeetings(meeting.id) } }),
    respondMeeting: useMutation({ mutationFn: ({ id, campusPresence, availableRangesJson, note }: { id: string; campusPresence: string; availableRangesJson: string; note?: string }) => respondToMeeting(id, { campusPresence, availableRangesJson, note }), retry: false, onSuccess: async (meeting) => { appToast.success('Availability submitted.'); await invalidateMeetings(meeting.id) } }),
    confirmMeeting: useMutation({ mutationFn: ({ id, startAt, endAt, location }: { id: string; startAt: string; endAt: string; location?: string }) => confirmMeeting(id, { startAt, endAt, location }), onSuccess: async (meeting) => invalidateMeetings(meeting.id) }),
    cancelMeeting: useMutation({ mutationFn: cancelMeeting, onSuccess: async (meeting) => invalidateMeetings(meeting.id) }),
    updateNotes: useMutation({ mutationFn: ({ id, title, agenda, notes }: { id: string; title?: string; agenda?: string; notes?: string }) => updateMeetingNotes(id, { title, agenda, notes }), onSuccess: async (meeting) => invalidateMeetings(meeting.id) }),
    addActionItem: useMutation({ mutationFn: ({ id, title, assignedStudentId }: { id: string; title: string; assignedStudentId?: string }) => addMeetingActionItem(id, { title, assignedStudentId }), onSuccess: async (meeting) => invalidateMeetings(meeting.id) }),
    convertActionItem: useMutation({ mutationFn: ({ meetingId, actionItemId, payload }: { meetingId: string; actionItemId: string; payload: ActionItemTaskPayload }) => convertActionItemToTask(meetingId, actionItemId, payload), onSuccess: async (task) => { appToast.success('Task created from action item.'); await queryClient.invalidateQueries({ queryKey: queryKeys.tasks.detail(task.id) }) } }),
  }
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
