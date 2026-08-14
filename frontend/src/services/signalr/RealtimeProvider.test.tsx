import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { RealtimeProvider } from './RealtimeProvider'
import type { ReactNode } from 'react'

type MockAuthState = {
  status: 'authenticated' | 'unauthenticated'
  accessToken: string | null
  session: { sessionId: string } | null
}

const authState = vi.hoisted(() => ({
  value: {
    status: 'authenticated',
    accessToken: 'token-1',
    session: { sessionId: 'session-1' },
  } as MockAuthState,
}))

vi.mock('../../lib/auth/AuthProvider', () => ({
  useAuth: () => authState.value,
}))

type Handler = (...args: unknown[]) => void

function createFakeConnection() {
  const handlers = new Map<string, Handler>()
  return {
    state: 'Connected',
    on: vi.fn((name: string, handler: Handler) => handlers.set(name, handler)),
    off: vi.fn((name: string) => handlers.delete(name)),
    onreconnected: vi.fn(),
    start: vi.fn(() => Promise.resolve()),
    stop: vi.fn(() => Promise.resolve()),
    emit: (name: string, payload: unknown) => handlers.get(name)?.(payload),
  }
}

describe('RealtimeProvider', () => {
  beforeEach(() => {
    authState.value = {
      status: 'authenticated',
      accessToken: 'token-1',
      session: { sessionId: 'session-1' },
    }
  })

  it('starts one authenticated connection and does not duplicate subscriptions on rerender', async () => {
    const connection = createFakeConnection()
    const createConnection = vi.fn(() => connection as never)
    const queryClient = new QueryClient()
    const { rerender } = renderWithClient(queryClient, <RealtimeProvider createConnection={createConnection}><div>app</div></RealtimeProvider>)

    await waitFor(() => expect(connection.start).toHaveBeenCalledTimes(1))
    rerender(<QueryClientProvider client={queryClient}><RealtimeProvider createConnection={createConnection}><div>app</div></RealtimeProvider></QueryClientProvider>)

    expect(createConnection).toHaveBeenCalledTimes(1)
    expect(connection.on).toHaveBeenCalledTimes(1)
  })

  it('stops and removes subscriptions when the authenticated session ends', async () => {
    const connection = createFakeConnection()
    const queryClient = new QueryClient()
    const createConnection = vi.fn(() => connection as never)
    const { rerender } = renderWithClient(queryClient, <RealtimeProvider createConnection={createConnection}><div>app</div></RealtimeProvider>)
    await waitFor(() => expect(connection.start).toHaveBeenCalledTimes(1))

    authState.value = { status: 'unauthenticated', accessToken: null, session: null }
    rerender(<QueryClientProvider client={queryClient}><RealtimeProvider createConnection={createConnection}><div>app</div></RealtimeProvider></QueryClientProvider>)

    expect(connection.off).toHaveBeenCalledWith('NotificationCreated', expect.any(Function))
    expect(connection.stop).toHaveBeenCalledTimes(1)
  })
})

function renderWithClient(queryClient: QueryClient, children: ReactNode) {
  return render(<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>)
}
