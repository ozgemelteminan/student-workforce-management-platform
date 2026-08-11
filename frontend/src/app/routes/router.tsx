import { createBrowserRouter } from 'react-router-dom'
import { AppShell } from '../../components/layout/AppShell'
import { RequireAuth } from '../../features/auth/RequireAuth'
import { AppPlaceholderPage } from '../../pages/AppPlaceholderPage'
import { LoginPlaceholder } from '../../pages/auth/LoginPlaceholder'
import { UiShowcasePage } from '../../pages/dev/UiShowcasePage'
import { FoundationPage } from '../../pages/FoundationPage'
import { NotFoundPage } from '../../pages/NotFoundPage'

const devRoutes = import.meta.env.DEV ? [{ path: '/__dev/ui', element: <UiShowcasePage /> }] : []

export const router = createBrowserRouter([
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AppShell />,
        children: [
          { path: '/', element: <FoundationPage /> },
          { path: '/tasks', element: <AppPlaceholderPage title="Tasks" /> },
          { path: '/marketplace', element: <AppPlaceholderPage title="Marketplace" /> },
          { path: '/students', element: <AppPlaceholderPage title="Students" /> },
          { path: '/schedule', element: <AppPlaceholderPage title="Schedule" /> },
          { path: '/requests', element: <AppPlaceholderPage title="Requests" /> },
          { path: '/reviews', element: <AppPlaceholderPage title="Reviews" /> },
          { path: '/files', element: <AppPlaceholderPage title="Files" /> },
          { path: '/announcements', element: <AppPlaceholderPage title="Announcements" /> },
          { path: '/templates', element: <AppPlaceholderPage title="Templates" /> },
          { path: '/recurring-tasks', element: <AppPlaceholderPage title="Recurring Tasks" /> },
          { path: '/notifications', element: <AppPlaceholderPage title="Notifications" /> },
          { path: '/analytics', element: <AppPlaceholderPage title="Analytics" /> },
          { path: '/audit-logs', element: <AppPlaceholderPage title="Audit Logs" /> },
          { path: '/settings', element: <AppPlaceholderPage title="Settings" /> },
          ...devRoutes,
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
  {
    path: '/login',
    element: <LoginPlaceholder />,
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
])
