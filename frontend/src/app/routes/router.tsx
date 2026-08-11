import { createBrowserRouter } from 'react-router-dom'
import { RequireAuth } from '../../features/auth/RequireAuth'
import { LoginPlaceholder } from '../../pages/auth/LoginPlaceholder'
import { FoundationPage } from '../../pages/FoundationPage'
import { NotFoundPage } from '../../pages/NotFoundPage'

export const router = createBrowserRouter([
  {
    element: <RequireAuth />,
    children: [
      {
        path: '/',
        element: <FoundationPage />,
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
