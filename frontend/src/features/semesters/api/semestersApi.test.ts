import { afterEach, describe, expect, it, vi } from 'vitest'
import { createSemester, deactivateSemester, getSemesters, reactivateSemester, updateSemester } from './semestersApi'

describe('semesters api client contracts', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('supports active selector reads and admin lifecycle routes', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const request = input as Request
      if (request.method === 'PUT' || request.method === 'POST') {
        return Response.json({ id: 'semester-id', name: '2026-2027 Fall', startDate: '2026-09-01', endDate: '2027-01-15', status: 'ACTIVE', isActive: true, concurrencyToken: 'token' })
      }
      return Response.json([{ id: 'semester-id', name: '2026-2027 Fall', startDate: '2026-09-01', endDate: '2027-01-15', status: 'ACTIVE', isActive: true, concurrencyToken: 'token' }])
    })
    vi.stubGlobal('fetch', fetchMock)

    await getSemesters()
    await getSemesters(true)
    await createSemester({ name: '2026-2027 Fall', startDate: '2026-09-01', endDate: '2027-01-15', status: 'ACTIVE' })
    await updateSemester('semester-id', { name: '2026-2027 Fall', startDate: '2026-09-01', endDate: '2027-01-15', status: 'ACTIVE' })
    await deactivateSemester('semester-id')
    await reactivateSemester('semester-id')

    const urls = fetchMock.mock.calls.map((call) => {
      const request = call[0]
      if (!(request instanceof Request)) throw new Error('Expected Request')
      return `${request.method} ${request.url}`
    })
    expect(urls[0]).toContain('/api/v1/semesters?includeInactive=false')
    expect(urls[1]).toContain('/api/v1/semesters?includeInactive=true')
    expect(urls[2]).toContain('POST')
    expect(urls[3]).toContain('PUT')
    expect(urls[4]).toContain('/semesters/semester-id/deactivate')
    expect(urls[5]).toContain('/semesters/semester-id/reactivate')
  })
})
