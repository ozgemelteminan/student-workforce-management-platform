import { describe, expect, it } from 'vitest'
import { availabilityStatuses, dayOfWeekValues, formatTimeRange } from './timeOnly'

describe('time-only schedule helpers', () => {
  it('keeps weekday options in the canonical Monday-first order exactly once', () => {
    expect(dayOfWeekValues).toEqual(['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'])
    expect(new Set(dayOfWeekValues).size).toBe(dayOfWeekValues.length)
  })

  it('formats wall-clock time ranges without timezone conversion', () => {
    expect(formatTimeRange('9:00', '10:30')).toBe('09:00-10:30')
  })

  it('keeps availability statuses aligned with the backend enum exactly once', () => {
    expect(availabilityStatuses).toEqual(['AVAILABLE', 'UNAVAILABLE', 'PREFERRED'])
    expect(new Set(availabilityStatuses).size).toBe(availabilityStatuses.length)
  })
})
