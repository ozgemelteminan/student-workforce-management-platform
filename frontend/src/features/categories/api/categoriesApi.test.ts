import { afterEach, describe, expect, it, vi } from 'vitest'
import { createCategory, deactivateCategory, getCategories, reactivateCategory, updateCategory } from './categoriesApi'

describe('categories api client contracts', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('supports active selector reads and admin management routes', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const request = input as Request
      if (request.method === 'PUT' || request.method === 'POST') {
        return Response.json({ id: 'category-id', name: 'Administrative', isActive: true })
      }
      return Response.json([{ id: 'category-id', name: 'Administrative', isActive: true }])
    })
    vi.stubGlobal('fetch', fetchMock)

    await getCategories()
    await getCategories(true)
    await createCategory({ name: 'Administrative' })
    await updateCategory('category-id', { name: 'Admin' })
    await deactivateCategory('category-id')
    await reactivateCategory('category-id')

    const urls = fetchMock.mock.calls.map((call) => {
      const request = call[0]
      if (!(request instanceof Request)) throw new Error('Expected Request')
      return `${request.method} ${request.url}`
    })
    expect(urls[0]).toContain('GET')
    expect(urls[0]).toContain('/api/v1/categories')
    expect(urls[0]).not.toContain('includeInactive=true')
    expect(urls[1]).toContain('/api/v1/categories?includeInactive=true')
    expect(urls[2]).toContain('POST')
    expect(urls[3]).toContain('PUT')
    expect(urls[4]).toContain('/categories/category-id/deactivate')
    expect(urls[5]).toContain('/categories/category-id/reactivate')
  })
})
