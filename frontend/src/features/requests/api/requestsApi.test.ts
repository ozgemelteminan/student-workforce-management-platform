import { afterEach, describe, expect, it, vi } from 'vitest'
import { approveRequest, getRequests } from './requestsApi'

describe('requests api client contracts', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('loads default queue filters and preserves human-readable labels', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const request = input as Request
      if (request.method === 'POST') {
        return Response.json({ id: 'request-id', taskId: 'task-id', requestedById: 'student-id', type: 'EXTENSION', reason: 'Need time', status: 'APPROVED', createdAt: '2026-08-15T12:00:00Z', concurrencyToken: 'token', taskTitle: 'TOEFL Score Upload', requestedByName: 'Test Student' })
      }
      return Response.json({ items: [{ id: 'request-id', taskId: 'task-id', requestedById: 'student-id', type: 'EXTENSION', reason: 'Need time', status: 'PENDING', createdAt: '2026-08-15T12:00:00Z', concurrencyToken: 'token', taskTitle: 'TOEFL Score Upload', requestedByName: 'Test Student' }], page: 1, pageSize: 20, totalCount: 1, totalPages: 1, hasNextPage: false, hasPreviousPage: false })
    })
    vi.stubGlobal('fetch', fetchMock)

    const queue = await getRequests({ page: 1, pageSize: 20 })
    const approved = await approveRequest('request-id', 'Approved')

    expect(queue.items[0]?.taskTitle).toBe('TOEFL Score Upload')
    expect(queue.items[0]?.requestedByName).toBe('Test Student')
    expect(approved.status).toBe('APPROVED')
    const listRequest = fetchMock.mock.calls[0]?.[0]
    const approveRequestCall = fetchMock.mock.calls[1]?.[0]
    if (!(listRequest instanceof Request) || !(approveRequestCall instanceof Request)) throw new Error('Expected request API calls to issue Requests')
    expect(listRequest.url).toContain('/api/v1/requests?page=1&pageSize=20')
    expect(listRequest.url).not.toContain('type=null')
    expect(listRequest.url).not.toContain('status=null')
    expect(approveRequestCall.url).toContain('/api/v1/requests/request-id/approve')
  })
})
