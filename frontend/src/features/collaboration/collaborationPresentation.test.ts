import { describe, expect, it } from 'vitest'
import { campusPresenceLabels, meetingStatusLabels, meetingTypeLabels, timesheetStatusLabels } from './collaborationPresentation'

describe('collaboration presentation labels', () => {
  it('formats meeting enum values without raw enum text', () => {
    expect(meetingTypeLabels.IN_PERSON).toBe('In person')
    expect(meetingTypeLabels.ONLINE).toBe('Online')
    expect(meetingTypeLabels.HYBRID).toBe('Hybrid')
    expect(meetingStatusLabels.AVAILABILITY_REQUESTED).toBe('Availability requested')
    expect(campusPresenceLabels.ON_CAMPUS).toBe('On campus')
    expect(timesheetStatusLabels.NEEDS_CORRECTION).toBe('Needs correction')

    const labels = [
      ...Object.values(meetingTypeLabels),
      ...Object.values(meetingStatusLabels),
      ...Object.values(campusPresenceLabels),
      ...Object.values(timesheetStatusLabels),
    ]

    expect(labels.every((label) => !label.includes('_'))).toBe(true)
  })
})
