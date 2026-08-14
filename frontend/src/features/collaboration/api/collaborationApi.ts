import { apiRequest } from '../../../lib/api'
import type { ActionItemTaskPayload, CreateMeetingPayload, Meeting, MeetingSlotRecommendation, MeetingStatus, PaginatedResult, TemporaryUnavailability, TimesheetStatus, TimesheetWeek } from '../types'
import type { Task } from '../../tasks/types'

function params(filters: Record<string, string | number | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function getCurrentTimesheet(signal?: AbortSignal) {
  return apiRequest<TimesheetWeek>('/timesheets/current', { signal })
}

export function getTimesheets(filters: { page: number; pageSize: number; studentId?: string; status?: TimesheetStatus }, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<TimesheetWeek>>(`/timesheets${params(filters)}`, { signal })
}

export function upsertTimesheetEntry(payload: { entryId?: string; taskId: string; workDate: string; minutes: number; note?: string }) {
  const body = { taskId: payload.taskId, workDate: payload.workDate, minutes: payload.minutes, note: payload.note }
  return payload.entryId
    ? apiRequest<TimesheetWeek>(`/timesheets/entries/${payload.entryId}`, { method: 'PUT', body })
    : apiRequest<TimesheetWeek>('/timesheets/entries', { method: 'POST', body })
}

export function deleteTimesheetEntry(entryId: string) {
  return apiRequest<TimesheetWeek>(`/timesheets/entries/${entryId}`, { method: 'DELETE' })
}

export function submitTimesheet(id: string) {
  return apiRequest<TimesheetWeek>(`/timesheets/${id}/submit`, { method: 'POST' })
}

export function reviewTimesheet(id: string, status: Extract<TimesheetStatus, 'APPROVED' | 'NEEDS_CORRECTION'>, reviewerComment?: string) {
  return apiRequest<TimesheetWeek>(`/timesheets/${id}/review`, { method: 'POST', body: { status, reviewerComment } })
}

export function getUnavailability(filters: { studentId?: string } = {}, signal?: AbortSignal) {
  return apiRequest<TemporaryUnavailability[]>(`/unavailability${params(filters)}`, { signal })
}

export function createUnavailability(payload: { startAt: string; endAt: string; category: string; note?: string }) {
  return apiRequest<TemporaryUnavailability>('/unavailability', { method: 'POST', body: payload })
}

export function deleteUnavailability(id: string) {
  return apiRequest<void>(`/unavailability/${id}`, { method: 'DELETE' })
}

export function getMeetings(filters: { page: number; pageSize: number; search?: string; status?: MeetingStatus }, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<Meeting>>(`/meetings${params(filters)}`, { signal })
}

export function getMeeting(id: string, signal?: AbortSignal) {
  return apiRequest<Meeting>(`/meetings/${id}`, { signal })
}

export function createMeeting(payload: CreateMeetingPayload) {
  return apiRequest<Meeting>('/meetings', { method: 'POST', body: payload })
}

export function respondToMeeting(id: string, payload: { campusPresence: string; availableRangesJson: string; note?: string }) {
  return apiRequest<Meeting>(`/meetings/${id}/response`, { method: 'POST', body: payload })
}

export function getMeetingSlots(id: string, signal?: AbortSignal) {
  return apiRequest<MeetingSlotRecommendation[]>(`/meetings/${id}/slot-recommendations`, { signal })
}

export function confirmMeeting(id: string, payload: { startAt: string; endAt: string; location?: string }) {
  return apiRequest<Meeting>(`/meetings/${id}/confirm`, { method: 'POST', body: payload })
}

export function updateMeetingNotes(id: string, payload: { agenda?: string; notes?: string }) {
  return apiRequest<Meeting>(`/meetings/${id}/notes`, { method: 'PUT', body: payload })
}

export function cancelMeeting(id: string) {
  return apiRequest<Meeting>(`/meetings/${id}/cancel`, { method: 'POST' })
}

export function addMeetingActionItem(id: string, payload: { title: string; assignedStudentId?: string }) {
  return apiRequest<Meeting>(`/meetings/${id}/action-items`, { method: 'POST', body: payload })
}

export function convertActionItemToTask(meetingId: string, actionItemId: string, payload: ActionItemTaskPayload) {
  return apiRequest<Task>(`/meetings/${meetingId}/action-items/${actionItemId}/create-task`, { method: 'POST', body: payload })
}
