import { afterEach, describe, expect, it, vi } from 'vitest'
import { getStudentSkills, upsertStudentSkill } from './studentsApi'

describe('students api client contracts', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('reads student skills through the student-scoped skills endpoint', async () => {
    const fetchMock = vi.fn(async () => Response.json([{ skillId: 'skill-id', name: 'Data QA', level: 'INTERMEDIATE' }]))
    vi.stubGlobal('fetch', fetchMock)

    const skills = await getStudentSkills('student-id')

    expect(skills).toEqual([{ skillId: 'skill-id', name: 'Data QA', level: 'INTERMEDIATE' }])
    const call = fetchMock.mock.calls.at(0) as [Request] | undefined
    const request = call?.[0]
    if (!(request instanceof Request)) {
      throw new Error('Expected getStudentSkills to issue a Request')
    }
    expect(request.method).toBe('GET')
    expect(request.url).toContain('/api/v1/students/student-id/skills')
  })

  it('upserts student skills through the same student-scoped collection', async () => {
    const fetchMock = vi.fn(async () => Response.json({ id: 'relationship-id', studentId: 'student-id', skillId: 'skill-id', level: 'EXPERT' }))
    vi.stubGlobal('fetch', fetchMock)

    await upsertStudentSkill('student-id', { skillId: 'skill-id', level: 'EXPERT' })

    const call = fetchMock.mock.calls.at(0) as [Request] | undefined
    const request = call?.[0]
    if (!(request instanceof Request)) {
      throw new Error('Expected upsertStudentSkill to issue a Request')
    }
    expect(request.method).toBe('POST')
    expect(request.url).toContain('/api/v1/students/student-id/skills')
    expect(await request.json()).toEqual({ skillId: 'skill-id', level: 'EXPERT' })
  })
})
