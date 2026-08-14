import { afterEach, describe, expect, it, vi } from 'vitest'
import { createTaskFromTemplate, createTemplate, getTemplates } from './templatesApi'

describe('templates api client contracts', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('uses the template collection endpoints and preserves create-task response ids', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const request = input as Request
      if (request.url.includes('/templates/template-id/create-task')) {
        expect(await request.json()).toEqual({ deadline: '2026-08-21T17:00:00Z' })
        return Response.json({ id: 'created-task-id' })
      }
      if (request.method === 'POST') {
        return Response.json({ id: 'template-id' })
      }
      return Response.json({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false })
    })
    vi.stubGlobal('fetch', fetchMock)

    await getTemplates({ page: 1, pageSize: 20, search: 'onboarding' })
    await createTemplate({ title: 'Onboarding', categoryId: 'category-id', defaultPriority: 'MEDIUM', defaultDifficulty: 'MEDIUM', estimatedDurationMinutes: 60 })
    const task = await createTaskFromTemplate('template-id', { deadline: '2026-08-21T17:00:00Z' })

    expect(task.id).toBe('created-task-id')
    expect(fetchMock).toHaveBeenCalledTimes(3)
    const listRequest = fetchMock.mock.calls[0]?.[0]
    const createTaskRequest = fetchMock.mock.calls[2]?.[0]
    if (!(listRequest instanceof Request) || !(createTaskRequest instanceof Request)) {
      throw new Error('Expected template API calls to issue Requests')
    }
    expect(listRequest.url).toContain('/api/v1/templates?')
    expect(createTaskRequest.url).toContain('/templates/template-id/create-task')
  })
})
