import { sanitizeReturnTo } from '../../src/features/auth/returnTo'

describe('sanitizeReturnTo', () => {
  it('accepts internal application paths', () => {
    expect(sanitizeReturnTo('/tasks?status=ASSIGNED#top')).toBe('/tasks?status=ASSIGNED#top')
  })

  it('rejects external, protocol-relative, auth, and malformed targets', () => {
    expect(sanitizeReturnTo('https://example.com/tasks')).toBe('/')
    expect(sanitizeReturnTo('//example.com/tasks')).toBe('/')
    expect(sanitizeReturnTo('/login')).toBe('/')
    expect(sanitizeReturnTo('/tasks\\evil')).toBe('/')
  })
})
