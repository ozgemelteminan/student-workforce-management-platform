import { formatDateOnly, formatIstanbulDateTime, parseDateOnly } from '../../src/lib/date-time'

describe('Istanbul timezone utilities', () => {
  it('formats UTC instants in Europe/Istanbul instead of browser local time', () => {
    expect(formatIstanbulDateTime('2026-08-11T21:30:00Z')).toBe('12.08.2026 00:30')
  })

  it('keeps date-only values on their intended calendar day', () => {
    expect(formatDateOnly('2026-08-11')).toBe('11.08.2026')
    expect(parseDateOnly('2026-08-11').toISOString()).toBe('2026-08-11T12:00:00.000Z')
  })
})

