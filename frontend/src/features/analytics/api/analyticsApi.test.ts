import { afterEach, describe, expect, it, vi } from 'vitest'
import { getDashboardAnalytics, getRequestAnalytics, getTasksByCategory, getTasksByStatus, getWorkloadDistribution } from './analyticsApi'

describe('analytics api client contracts', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('requests every operational analytics endpoint without client-side filters', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const request = input as Request
      if (request.url.endsWith('/analytics/dashboard')) return Response.json({ totalTasks: 1, activeTasks: 1, completedTasks: 0, overdueTasks: 0, pendingReviews: 0, pendingRequests: 0 })
      return Response.json([])
    })
    vi.stubGlobal('fetch', fetchMock)

    await Promise.all([getDashboardAnalytics(), getTasksByStatus(), getTasksByCategory(), getWorkloadDistribution(), getRequestAnalytics()])

    const urls = fetchMock.mock.calls.map(([input]) => (input as Request).url)
    expect(urls.some((url) => url.endsWith('/analytics/dashboard'))).toBe(true)
    expect(urls.some((url) => url.endsWith('/analytics/tasks/status'))).toBe(true)
    expect(urls.some((url) => url.endsWith('/analytics/tasks/category'))).toBe(true)
    expect(urls.some((url) => url.endsWith('/analytics/workload'))).toBe(true)
    expect(urls.some((url) => url.endsWith('/analytics/requests'))).toBe(true)
  })
})
