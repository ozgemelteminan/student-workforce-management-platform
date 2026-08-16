import { describe, expect, it } from 'vitest'
import { requestStatusLabels, requestTypeLabels } from './requestPresentation'

describe('request presentation labels', () => {
  it('formats request enum values for users', () => {
    expect(requestTypeLabels.EXTENSION).toBe('Extension')
    expect(requestTypeLabels.REASSIGNMENT).toBe('Reassignment')
    expect(requestStatusLabels.PENDING).toBe('Pending')

    expect([...Object.values(requestTypeLabels), ...Object.values(requestStatusLabels)].every((label) => !label.includes('_'))).toBe(true)
  })
})
