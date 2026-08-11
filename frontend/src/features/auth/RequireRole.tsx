import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { Button, ErrorState } from '../../components/ui'
import { useAuth } from '../../lib/auth/AuthProvider'
import type { UserRole } from '../../lib/auth/authTypes'

type RequireRoleProps = {
  roles: UserRole[]
}

export function RequireRole({ roles }: RequireRoleProps) {
  const auth = useAuth()
  const location = useLocation()

  if (auth.status === 'loading') {
    return (
      <main className="flex min-h-screen items-center justify-center bg-page text-sm text-text-secondary">
        Loading session...
      </main>
    )
  }

  if (auth.status === 'unauthenticated') {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  const allowed = auth.user?.roles.some((role) => roles.includes(role))
  if (!allowed) {
    return (
      <ErrorState
        variant="permission"
        title="Permission required"
        description="Your account is signed in, but this workspace area is not available for your role."
        navigationAction={<Button onClick={() => window.history.back()} variant="outline">Go back</Button>}
      />
    )
  }

  return <Outlet />
}
