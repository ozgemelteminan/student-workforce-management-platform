import { afterEach, describe, expect, it, vi } from 'vitest'
import { createSkill, deactivateSkill, getSkills, reactivateSkill, updateSkill } from './skillsApi'

describe('skills api client contracts', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('supports active selector reads and admin management routes', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const request = input as Request
      if (request.method === 'PUT' || request.method === 'POST') {
        return Response.json({ id: 'skill-id', name: 'Programming', isActive: true })
      }
      return Response.json([{ id: 'skill-id', name: 'Programming', isActive: true }])
    })
    vi.stubGlobal('fetch', fetchMock)

    await getSkills()
    await getSkills(true)
    await createSkill({ name: 'Programming' })
    await updateSkill('skill-id', { name: 'Web Development' })
    await deactivateSkill('skill-id')
    await reactivateSkill('skill-id')

    const urls = fetchMock.mock.calls.map((call) => {
      const request = call[0]
      if (!(request instanceof Request)) throw new Error('Expected Request')
      return `${request.method} ${request.url}`
    })
    expect(urls[0]).toContain('/api/v1/skills')
    expect(urls[0]).not.toContain('includeInactive=true')
    expect(urls[1]).toContain('/api/v1/skills?includeInactive=true')
    expect(urls[2]).toContain('POST')
    expect(urls[3]).toContain('PUT')
    expect(urls[4]).toContain('/skills/skill-id/deactivate')
    expect(urls[5]).toContain('/skills/skill-id/reactivate')
  })
})
