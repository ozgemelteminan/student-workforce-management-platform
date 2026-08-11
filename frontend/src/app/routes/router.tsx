import { createBrowserRouter } from 'react-router-dom'
import { AppShell } from '../../components/layout/AppShell'
import { RequireAuth } from '../../features/auth/RequireAuth'
import { RequireRole } from '../../features/auth/RequireRole'
import { AppPlaceholderPage } from '../../pages/AppPlaceholderPage'
import { AcceptInvitationPage } from '../../pages/auth/AcceptInvitationPage'
import { ForgotPasswordPage } from '../../pages/auth/ForgotPasswordPage'
import { LoginPage } from '../../pages/auth/LoginPage'
import { ResetPasswordPage } from '../../pages/auth/ResetPasswordPage'
import { SessionsPage } from '../../pages/auth/SessionsPage'
import { UiShowcasePage } from '../../pages/dev/UiShowcasePage'
import { FoundationPage } from '../../pages/FoundationPage'
import { NotFoundPage } from '../../pages/NotFoundPage'
import type { UserRole } from '../../lib/auth/authTypes'

const devRoutes = import.meta.env.DEV ? [{ path: '/__dev/ui', element: <UiShowcasePage /> }] : []
const allRoles: UserRole[] = ['ADMIN', 'TASK_MANAGER', 'REVIEWER', 'STUDENT']
const staffRoles: UserRole[] = ['ADMIN', 'TASK_MANAGER']
const reviewRoles: UserRole[] = ['ADMIN', 'REVIEWER']

function roleRoute(path: string, title: string, roles: UserRole[]) {
  return {
    path,
    element: <RequireRole roles={roles} />,
    children: [{ index: true, element: <AppPlaceholderPage title={title} /> }],
  }
}

export const router = createBrowserRouter([
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AppShell />,
        children: [
          { path: '/', element: <FoundationPage /> },
          roleRoute('/tasks', 'Tasks', allRoles),
          roleRoute('/marketplace', 'Marketplace', allRoles),
          roleRoute('/students', 'Students', staffRoles),
          roleRoute('/schedule', 'Schedule', allRoles),
          roleRoute('/requests', 'Requests', allRoles),
          roleRoute('/reviews', 'Reviews', reviewRoles),
          roleRoute('/files', 'Files', allRoles),
          roleRoute('/announcements', 'Announcements', allRoles),
          roleRoute('/templates', 'Templates', staffRoles),
          roleRoute('/recurring-tasks', 'Recurring Tasks', staffRoles),
          roleRoute('/notifications', 'Notifications', allRoles),
          roleRoute('/analytics', 'Analytics', staffRoles),
          roleRoute('/audit-logs', 'Audit Logs', ['ADMIN']),
          roleRoute('/settings', 'Settings', ['ADMIN']),
          { path: '/sessions', element: <SessionsPage /> },
          ...devRoutes,
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/forgot-password',
    element: <ForgotPasswordPage />,
  },
  {
    path: '/reset-password',
    element: <ResetPasswordPage />,
  },
  {
    path: '/invitations/accept',
    element: <AcceptInvitationPage />,
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
])
