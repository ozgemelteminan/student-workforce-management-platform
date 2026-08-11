import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import React from 'react'
import { MemoryRouter } from 'react-router-dom'
import { AuthProvider } from '../../src/lib/auth/AuthProvider'
import type { AuthSession } from '../../src/lib/auth/authTypes'
import { createAppQueryClient } from '../../src/lib/query'
import { ForgotPasswordPage } from '../../src/pages/auth/ForgotPasswordPage'
import { LoginPage } from '../../src/pages/auth/LoginPage'

function renderWithAuth(ui: React.ReactNode, initialEntry = '/') {
  const queryClient = createAppQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter initialEntries={[initialEntry]}>{ui}</MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>,
  )
}

function authResponse(): AuthSession {
  return {
    sessionId: '11111111-1111-1111-1111-111111111111',
    accessToken: tokenWithSession('11111111-1111-1111-1111-111111111111'),
    refreshToken: 'refresh-token',
    expiresAt: '2030-01-01T00:00:00Z',
    accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    refreshTokenExpiresAt: '2030-01-01T00:00:00Z',
    sessionExpiresAt: '2030-01-01T00:00:00Z',
    user: {
      id: '22222222-2222-2222-2222-222222222222',
      email: 'student@example.edu',
      displayName: 'Student User',
      roles: ['STUDENT'],
    },
  }
}

function tokenWithSession(sessionId: string) {
  const encoded = btoa(JSON.stringify({ sid: sessionId })).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  return `header.${encoded}.signature`
}

describe('auth pages', () => {
  afterEach(() => {
    sessionStorage.clear()
    vi.unstubAllGlobals()
  })

  it('submits login through the backend auth contract and stores the returned session', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(JSON.stringify(authResponse()), { status: 200, headers: { 'content-type': 'application/json' } }),
    )
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()
    renderWithAuth(<LoginPage />, '/login?returnTo=/tasks')

    await user.type(screen.getByLabelText(/email/i), 'student@example.edu')
    await user.type(screen.getByLabelText(/^password/i), 'Password1')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1))
    const request = fetchMock.mock.calls[0]?.[0] as Request
    expect(request.url).toContain('/api/v1/auth/login')
    expect(await request.json()).toMatchObject({ email: 'student@example.edu', password: 'Password1' })
    expect(sessionStorage.getItem('swm.auth.session')).toContain('student@example.edu')
  })

  it('shows enumeration-safe login failure text', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(
        new Response(JSON.stringify({ title: 'Forbidden' }), { status: 403, headers: { 'content-type': 'application/json' } }),
      ),
    )
    const user = userEvent.setup()
    renderWithAuth(<LoginPage />, '/login')

    await user.type(screen.getByLabelText(/email/i), 'missing@example.edu')
    await user.type(screen.getByLabelText(/^password/i), 'Password1')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    expect(await screen.findByText('Invalid email or password.')).toBeInTheDocument()
    expect(screen.queryByText(/email exists/i)).not.toBeInTheDocument()
  })

  it('keeps forgot-password success enumeration safe', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(
        new Response(JSON.stringify({ email: 'missing@example.edu', expiresAt: '2030-01-01T00:00:00Z' }), {
          status: 200,
          headers: { 'content-type': 'application/json' },
        }),
      ),
    )
    const user = userEvent.setup()
    renderWithAuth(<ForgotPasswordPage />, '/forgot-password')

    await user.type(screen.getByLabelText(/email/i), 'missing@example.edu')
    await user.click(screen.getByRole('button', { name: /send reset/i }))

    expect(await screen.findByText(/If an account exists for this email/i)).toBeInTheDocument()
  })

  it('handles forgot-password rate limits without a fabricated countdown', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(
        new Response(JSON.stringify({ title: 'Too many requests' }), { status: 429, headers: { 'content-type': 'application/json' } }),
      ),
    )
    const user = userEvent.setup()
    renderWithAuth(<ForgotPasswordPage />, '/forgot-password')

    await user.type(screen.getByLabelText(/email/i), 'student@example.edu')
    await user.click(screen.getByRole('button', { name: /send reset/i }))

    expect(await screen.findByText('Too many password reset requests. Please try again later.')).toBeInTheDocument()
    expect(screen.queryByText(/Retry after/)).not.toBeInTheDocument()
  })
})
