import { describe, expect, it, vi } from 'vitest'
import { useMutation } from '@tanstack/react-query'
import { useCollaborationMutations } from './useCollaborationQueries'

vi.mock('@tanstack/react-query', () => ({
  useMutation: vi.fn((options) => options),
  useQuery: vi.fn(),
  useQueryClient: () => ({ invalidateQueries: vi.fn() }),
}))

vi.mock('../../lib/toast', () => ({
  appToast: { success: vi.fn(), error: vi.fn(), info: vi.fn() },
}))

vi.mock('./api/collaborationApi', () => ({
  addMeetingActionItem: vi.fn(),
  cancelMeeting: vi.fn(),
  confirmMeeting: vi.fn(),
  convertActionItemToTask: vi.fn(),
  createMeeting: vi.fn(),
  createUnavailability: vi.fn(),
  deleteTimesheetEntry: vi.fn(),
  deleteUnavailability: vi.fn(),
  getCurrentTimesheet: vi.fn(),
  getMeeting: vi.fn(),
  getMeetings: vi.fn(),
  getMeetingSlots: vi.fn(),
  getTimesheets: vi.fn(),
  getUnavailability: vi.fn(),
  respondToMeeting: vi.fn(),
  reviewTimesheet: vi.fn(),
  submitTimesheet: vi.fn(),
  updateMeetingNotes: vi.fn(),
  upsertTimesheetEntry: vi.fn(),
}))

describe('collaboration query hooks', () => {
  it('does not retry meeting response mutations after 4xx failures', () => {
    const mutations = useCollaborationMutations() as unknown as { respondMeeting: { retry: unknown } }

    expect(mutations.respondMeeting.retry).toBe(false)
    expect(useMutation).toHaveBeenCalled()
  })
})
