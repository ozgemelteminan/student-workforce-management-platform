import { afterEach, describe, expect, it, vi } from 'vitest'
import { getMarketplaceListings } from './marketplaceApi'

describe('marketplace api client contracts', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('uses embedded task summary data from the marketplace contract without task detail enrichment', async () => {
    const fetchMock = vi.fn(async (_input: RequestInfo | URL, _init?: RequestInit) => Response.json({
      items: [{
        id: 'listing-id',
        taskId: 'task-id',
        status: 'PUBLISHED',
        approvalMode: 'MANUAL_APPROVAL',
        concurrencyToken: 'token',
        taskSummary: {
          taskId: 'task-id',
          title: 'Marketplace title',
          description: 'Safe summary',
          categoryId: 'category-id',
          categoryName: 'Operations',
          priority: 'HIGH',
          deadline: '2026-08-20T09:00:00Z',
          estimatedDurationMinutes: 90,
          requiredSkills: [{ skillId: 'skill-id', skillName: 'Data QA', minimumLevel: 'INTERMEDIATE' }],
        },
      }],
      page: 1,
      pageSize: 12,
      totalCount: 1,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    }))
    vi.stubGlobal('fetch', fetchMock)

    const result = await getMarketplaceListings({ page: 1, pageSize: 12, status: 'PUBLISHED' })

    expect(result.items[0]?.taskSummary?.title).toBe('Marketplace title')
    expect(result.items[0]?.taskSummary?.requiredSkills[0]?.skillName).toBe('Data QA')
    expect(fetchMock).toHaveBeenCalledTimes(1)
    const request = fetchMock.mock.calls[0]?.[0]
    if (!(request instanceof Request)) {
      throw new Error('Expected marketplace API call to issue a Request')
    }
    expect(request.url).toContain('/marketplace/listings')
  })
})
