import { describe, expect, it } from 'vitest'
import { formatTimeRange, normalizeTimeOnly } from './timeOnly'

describe('time-only formatting', () => {
  it('renders TimeOnly strings without applying browser timezone conversion', () => {
    expect(normalizeTimeOnly('09:00:00')).toBe('09:00')
    expect(normalizeTimeOnly('14:00')).toBe('14:00')
    expect(formatTimeRange('23:30:00', '23:59:00')).toBe('23:30-23:59')
  })
})
