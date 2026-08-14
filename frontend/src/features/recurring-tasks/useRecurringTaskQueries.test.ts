import { describe, expect, it } from 'vitest'
import { describeRecurrence } from './useRecurringTaskQueries'

describe('recurring task presentation', () => {
  it('renders frequency with optional local run time only from API fields', () => {
    expect(describeRecurrence('Daily', '09:00')).toBe('Daily at 09:00')
    expect(describeRecurrence('Weekly')).toBe('Weekly')
  })
})
