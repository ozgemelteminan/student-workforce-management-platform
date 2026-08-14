import { describe, expect, it } from 'vitest'
import { formatMetadata } from './useAuditQueries'

describe('audit metadata formatting', () => {
  it('formats JSON safely as text and leaves non-json text unchanged', () => {
    expect(formatMetadata('{"action":"Updated"}')).toContain('"action": "Updated"')
    expect(formatMetadata('<strong>not html</strong>')).toBe('<strong>not html</strong>')
    expect(formatMetadata()).toBe('None')
  })
})
