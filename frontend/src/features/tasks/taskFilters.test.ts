import { describe, expect, it } from 'vitest'
import { filtersFromSearchParams, filtersToSearchParams } from './taskFilters'

describe('task filter URL state', () => {
  it('parses supported filters and falls back from invalid values', () => {
    const filters = filtersFromSearchParams(new URLSearchParams('view=needs-review&page=2&pageSize=50&status=NOT_REAL&priority=HIGH&sortBy=created&sortDirection=sideways&search=db'))

    expect(filters).toMatchObject({
      view: 'needs-review',
      page: 2,
      pageSize: 50,
      search: 'db',
      priority: 'HIGH',
      sortBy: 'created',
      status: 'SUBMITTED_FOR_REVIEW',
    })
    expect(filters.sortDirection).toBeUndefined()
  })

  it('serializes only meaningful filter values', () => {
    const params = filtersToSearchParams({ view: 'overdue', page: 1, pageSize: 20, status: 'OVERDUE', sortBy: 'deadline', sortDirection: 'asc' })

    expect(params.get('view')).toBe('overdue')
    expect(params.get('status')).toBe('OVERDUE')
    expect(params.get('sortBy')).toBe('deadline')
    expect(params.get('sortDirection')).toBe('asc')
  })

  it('serializes the unassigned view as a server-side assignment filter', () => {
    const filters = filtersFromSearchParams(new URLSearchParams('view=unassigned&page=3'))
    const params = filtersToSearchParams(filters)

    expect(filters.view).toBe('unassigned')
    expect(filters.isAssigned).toBe(false)
    expect(params.get('view')).toBe('unassigned')
    expect(params.get('isAssigned')).toBe('false')
    expect(params.get('page')).toBe('3')
  })
})
