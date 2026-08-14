import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../../../lib/api'
import { confirmMeeting, createMeeting, createUnavailability, getCurrentTimesheet, getMeetingSlots, getMeetings, getTimesheets, respondToMeeting, reviewTimesheet, submitTimesheet, upsertTimesheetEntry } from './collaborationApi'

vi.mock('../../../lib/api', () => ({
  apiRequest: vi.fn(),
}))

const mockedApiRequest = vi.mocked(apiRequest)

describe('collaboration API client', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockedApiRequest.mockResolvedValue({})
  })

  it('calls weekly workload and review endpoints with canonical paths', async () => {
    await getCurrentTimesheet()
    await getTimesheets({ page: 1, pageSize: 20, status: 'SUBMITTED' })
    await upsertTimesheetEntry({ taskId: 'task-1', workDate: '2026-08-10', minutes: 45, note: 'handoff' })
    await upsertTimesheetEntry({ entryId: 'entry-1', taskId: 'task-1', workDate: '2026-08-11', minutes: 30 })
    await submitTimesheet('week-1')
    await reviewTimesheet('week-1', 'APPROVED')

    expect(mockedApiRequest).toHaveBeenNthCalledWith(1, '/timesheets/current', { signal: undefined })
    expect(mockedApiRequest).toHaveBeenNthCalledWith(2, '/timesheets?page=1&pageSize=20&status=SUBMITTED', { signal: undefined })
    expect(mockedApiRequest).toHaveBeenNthCalledWith(3, '/timesheets/entries', { method: 'POST', body: { taskId: 'task-1', workDate: '2026-08-10', minutes: 45, note: 'handoff' } })
    expect(mockedApiRequest).toHaveBeenNthCalledWith(4, '/timesheets/entries/entry-1', { method: 'PUT', body: { taskId: 'task-1', workDate: '2026-08-11', minutes: 30, note: undefined } })
    expect(mockedApiRequest).toHaveBeenNthCalledWith(5, '/timesheets/week-1/submit', { method: 'POST' })
    expect(mockedApiRequest).toHaveBeenNthCalledWith(6, '/timesheets/week-1/review', { method: 'POST', body: { status: 'APPROVED', reviewerComment: undefined } })
  })

  it('calls unavailability and meeting coordination endpoints without frontend persistence', async () => {
    await createUnavailability({ startAt: '2026-08-10T09:00:00Z', endAt: '2026-08-10T12:00:00Z', category: 'Exam' })
    await getMeetings({ page: 1, pageSize: 10, status: 'AVAILABILITY_REQUESTED' })
    await createMeeting({ title: 'Planning', type: 'IN_PERSON', responseDeadline: '2026-08-11T09:00:00Z', participantStudentIds: ['student-1'], location: 'Campus' })
    await respondToMeeting('meeting-1', { campusPresence: 'ON_CAMPUS', availableRangesJson: '[]' })
    await getMeetingSlots('meeting-1')
    await confirmMeeting('meeting-1', { startAt: '2026-08-11T10:00:00Z', endAt: '2026-08-11T11:00:00Z' })

    expect(mockedApiRequest).toHaveBeenNthCalledWith(1, '/unavailability', { method: 'POST', body: { startAt: '2026-08-10T09:00:00Z', endAt: '2026-08-10T12:00:00Z', category: 'Exam' } })
    expect(mockedApiRequest).toHaveBeenNthCalledWith(2, '/meetings?page=1&pageSize=10&status=AVAILABILITY_REQUESTED', { signal: undefined })
    expect(mockedApiRequest).toHaveBeenNthCalledWith(3, '/meetings', { method: 'POST', body: { title: 'Planning', type: 'IN_PERSON', responseDeadline: '2026-08-11T09:00:00Z', participantStudentIds: ['student-1'], location: 'Campus' } })
    expect(mockedApiRequest).toHaveBeenNthCalledWith(4, '/meetings/meeting-1/response', { method: 'POST', body: { campusPresence: 'ON_CAMPUS', availableRangesJson: '[]' } })
    expect(mockedApiRequest).toHaveBeenNthCalledWith(5, '/meetings/meeting-1/slot-recommendations', { signal: undefined })
    expect(mockedApiRequest).toHaveBeenNthCalledWith(6, '/meetings/meeting-1/confirm', { method: 'POST', body: { startAt: '2026-08-11T10:00:00Z', endAt: '2026-08-11T11:00:00Z' } })
  })
})
