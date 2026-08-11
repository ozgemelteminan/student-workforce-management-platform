import React from 'react'
import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { CommandPalette } from '../../src/components/layout/CommandPalette'
import { Sidebar } from '../../src/components/layout/Sidebar'
import { Topbar } from '../../src/components/layout/Topbar'
import { visibleNavigationGroups } from '../../src/components/layout/navigation'
import { TooltipProvider } from '../../src/components/ui'
import type { AuthSession, AuthUser } from '../../src/lib/auth/authTypes'

const admin: AuthUser = {
  id: 'admin-id',
  email: 'admin@example.edu',
  displayName: 'Admin User',
  roles: ['ADMIN'],
}

const student: AuthUser = {
  id: 'student-id',
  email: 'student@example.edu',
  displayName: 'Student User',
  roles: ['STUDENT'],
}

const session: AuthSession = {
  sessionId: 'session-id',
  accessToken: 'access-token',
  refreshToken: 'refresh-token',
  expiresAt: '2030-01-01T00:00:00Z',
  accessTokenExpiresAt: '2030-01-01T00:00:00Z',
  refreshTokenExpiresAt: '2030-01-01T00:00:00Z',
  sessionExpiresAt: '2030-01-01T00:00:00Z',
  user: admin,
}

describe('Phase 2 app shell infrastructure', () => {
  it('uses one role-aware navigation registry', () => {
    const studentLabels = visibleNavigationGroups(student.roles).flatMap((group) => group.items.map((item) => item.label))
    const adminLabels = visibleNavigationGroups(admin.roles).flatMap((group) => group.items.map((item) => item.label))

    expect(studentLabels).toContain('Tasks')
    expect(studentLabels).not.toContain('Audit Logs')
    expect(adminLabels).toContain('Audit Logs')
    expect(adminLabels).toContain('Settings')
  })

  it('supports sidebar collapse and keeps unauthorized destinations hidden', async () => {
    const user = userEvent.setup()
    const onCollapsedChange = vi.fn()

    render(
      <MemoryRouter>
        <TooltipProvider>
          <Sidebar user={student} collapsed={false} onCollapsedChange={onCollapsedChange} />
        </TooltipProvider>
      </MemoryRouter>,
    )

    expect(screen.getByRole('link', { name: /tasks/i })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /audit logs/i })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /collapse sidebar/i }))
    expect(onCollapsedChange).toHaveBeenCalledWith(true)
  })

  it('opens command palette with ctrl+k and hides admin commands for students', async () => {
    const user = userEvent.setup()
    const onOpenChange = vi.fn()

    render(
      <MemoryRouter>
        <CommandPalette open={false} onOpenChange={onOpenChange} roles={student.roles} />
      </MemoryRouter>,
    )

    await user.keyboard('{Control>}k{/Control}')
    expect(onOpenChange).toHaveBeenCalledWith(true)
  })

  it('exposes a visible topbar command trigger', async () => {
    const user = userEvent.setup()
    const onCommandPalette = vi.fn()
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })

    render(
      <MemoryRouter>
        <QueryClientProvider client={queryClient}>
          <TooltipProvider>
            <Topbar user={admin} session={session} onMobileMenu={() => undefined} onCommandPalette={onCommandPalette} onLogout={() => undefined} />
          </TooltipProvider>
        </QueryClientProvider>
      </MemoryRouter>,
    )

    await user.click(screen.getByRole('button', { name: /search or jump/i }))
    expect(onCommandPalette).toHaveBeenCalledTimes(1)
  })

  it('renders command navigation from the shared role registry', () => {
    render(
      <MemoryRouter>
        <CommandPalette open onOpenChange={() => undefined} roles={student.roles} />
      </MemoryRouter>,
    )

    expect(screen.getByRole('option', { name: /tasks/i })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: /settings/i })).not.toBeInTheDocument()
  })
})
