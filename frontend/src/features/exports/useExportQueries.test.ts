import { describe, expect, it } from 'vitest'
import { isProcessingStatus, isTerminalStatus } from './useExportQueries'

describe('export status lifecycle helpers', () => {
  it('identifies processing and terminal export states from the backend enum', () => {
    expect(isProcessingStatus('QUEUED')).toBe(true)
    expect(isProcessingStatus('PROCESSING')).toBe(true)
    expect(isProcessingStatus('COMPLETED')).toBe(false)
    expect(isTerminalStatus('COMPLETED')).toBe(true)
    expect(isTerminalStatus('FAILED')).toBe(true)
    expect(isTerminalStatus('EXPIRED')).toBe(true)
  })
})
