import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import React from 'react'
import { AuthProvider } from '../../src/lib/auth/AuthProvider'
import type { AuthSession } from '../../src/lib/auth/authTypes'
import { createAppQueryClient } from '../../src/lib/query'
import { SessionsPage } from '../../src/pages/auth/SessionsPage'

const session: AuthSession = {
  sessionId: '11111111-1111-1111-1111-111111111111',
  accessToken: 'header.payload.signature',
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

describe('SessionsPage', () => {
  afterEach(() => {
    sessionStorage.clear()
    vi.unstubAllGlobals()
  })

  it('lists backend sessions and marks the current session', async () => {
    sessionStorage.setItem('swm.auth.session', JSON.stringify(session))
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(
        new Response(
          JSON.stringify([
            {
              id: session.sessionId,
              userId: session.user.id,
              deviceName: 'Browser',
              ipAddress: null,
              expiresAt: '2030-01-01T00:00:00Z',
              revokedAt: null,
              createdAt: '2029-12-31T00:00:00Z',
            },
          ]),
          { status: 200, headers: { 'content-type': 'application/json' } },
        ),
      ),
    )

    render(
      <QueryClientProvider client={createAppQueryClient()}>
        <AuthProvider>
          <SessionsPage />
        </AuthProvider>
      </QueryClientProvider>,
    )

    expect(await screen.findByText('Browser')).toBeInTheDocument()
    expect(screen.getByText('Current session')).toBeInTheDocument()
  })
})
