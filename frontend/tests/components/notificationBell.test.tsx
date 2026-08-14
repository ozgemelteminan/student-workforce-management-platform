import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { NotificationBell } from '../../src/components/layout/NotificationBell'
import { TooltipProvider } from '../../src/components/ui'

describe('NotificationBell preferences', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads persisted preference values and saves through the preferences endpoint', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const request = input as Request
      if (request.url.includes('/notifications/unread-count')) {
        return Response.json({ count: 0 })
      }
      if (request.url.includes('/notifications/preferences') && request.method === 'GET') {
        return Response.json([
          { preferenceType: 'TaskAssigned', channel: 'EMAIL', isEnabled: false },
        ])
      }
      if (request.url.includes('/notifications/preferences') && request.method === 'PUT') {
        expect(await request.json()).toEqual({ preferenceType: 'TaskAssigned', channel: 'EMAIL', isEnabled: true })
        return Response.json({ id: 'preference-id', userId: 'user-id', preferenceType: 'TaskAssigned', channel: 'EMAIL', isEnabled: true })
      }
      if (request.url.includes('/notifications')) {
        return Response.json({ items: [], page: 1, pageSize: 8, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false })
      }
      return Response.json({})
    })
    vi.stubGlobal('fetch', fetchMock)
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })

    render(
      <MemoryRouter>
        <QueryClientProvider client={queryClient}>
          <TooltipProvider>
            <NotificationBell />
          </TooltipProvider>
        </QueryClientProvider>
      </MemoryRouter>,
    )

    await user.click(screen.getByRole('button', { name: /open notifications/i }))
    await user.click(screen.getByRole('button', { name: /preferences/i }))

    expect((await screen.findAllByText('Not configured')).length).toBeGreaterThan(0)
    const enabledCheckbox = await screen.findByRole('checkbox', { name: /enabled/i })
    expect(enabledCheckbox).not.toBeChecked()

    await user.click(enabledCheckbox)

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(expect.any(Request))
      expect(fetchMock.mock.calls.some(([input]) => input instanceof Request && input.method === 'PUT' && input.url.includes('/notifications/preferences'))).toBe(true)
    })
  })
})
