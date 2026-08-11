import { lazy, Suspense } from 'react'
import { createBrowserRouter } from 'react-router-dom'
import { AppShell } from '../../components/layout/AppShell'
import { Skeleton } from '../../components/ui'
import { RequireAuth } from '../../features/auth/RequireAuth'
import { RequireRole } from '../../features/auth/RequireRole'
import { AppPlaceholderPage } from '../../pages/AppPlaceholderPage'
import { AcceptInvitationPage } from '../../pages/auth/AcceptInvitationPage'
import { ForgotPasswordPage } from '../../pages/auth/ForgotPasswordPage'
import { LoginPage } from '../../pages/auth/LoginPage'
import { ResetPasswordPage } from '../../pages/auth/ResetPasswordPage'
import { SessionsPage } from '../../pages/auth/SessionsPage'
import { UiShowcasePage } from '../../pages/dev/UiShowcasePage'
import { NotFoundPage } from '../../pages/NotFoundPage'
import type { UserRole } from '../../lib/auth/authTypes'
import type { ReactNode } from 'react'

const DashboardPage = lazy(() => import('../../pages/dashboard/DashboardPage').then((module) => ({ default: module.DashboardPage })))
const TasksPage = lazy(() => import('../../pages/tasks/TasksPage').then((module) => ({ default: module.TasksPage })))
const TaskFormPage = lazy(() => import('../../pages/tasks/TaskFormPage').then((module) => ({ default: module.TaskFormPage })))
const TaskDetailPage = lazy(() => import('../../pages/tasks/TaskDetailPage').then((module) => ({ default: module.TaskDetailPage })))
const MarketplacePage = lazy(() => import('../../pages/marketplace/MarketplacePage').then((module) => ({ default: module.MarketplacePage })))
const FocusModePage = lazy(() => import('../../pages/tasks/FocusModePage').then((module) => ({ default: module.FocusModePage })))
const StudentsPage = lazy(() => import('../../pages/students/StudentsPage').then((module) => ({ default: module.StudentsPage })))
const StudentDetailPage = lazy(() => import('../../pages/students/StudentDetailPage').then((module) => ({ default: module.StudentDetailPage })))
const SchedulePage = lazy(() => import('../../pages/schedule/SchedulePage').then((module) => ({ default: module.SchedulePage })))
const RequestsPage = lazy(() => import('../../pages/requests/RequestsPage').then((module) => ({ default: module.RequestsPage })))
const ReviewsPage = lazy(() => import('../../pages/reviews/ReviewsPage').then((module) => ({ default: module.ReviewsPage })))
const FilesPage = lazy(() => import('../../pages/files/FilesPage').then((module) => ({ default: module.FilesPage })))
const AnnouncementsPage = lazy(() => import('../../pages/announcements/AnnouncementsPage').then((module) => ({ default: module.AnnouncementsPage })))
const AnnouncementDetailPage = lazy(() => import('../../pages/announcements/AnnouncementDetailPage').then((module) => ({ default: module.AnnouncementDetailPage })))

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

function lazyPage(element: ReactNode) {
  return <Suspense fallback={<div className="space-y-3"><Skeleton className="h-8 w-56" /><Skeleton className="h-64" /></div>}>{element}</Suspense>
}

export const router = createBrowserRouter([
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AppShell />,
        children: [
          { path: '/', element: lazyPage(<DashboardPage />) },
          { path: '/tasks', element: <RequireRole roles={allRoles} />, children: [{ index: true, element: lazyPage(<TasksPage />) }, { path: 'new', element: <RequireRole roles={staffRoles} />, children: [{ index: true, element: lazyPage(<TaskFormPage />) }] }, { path: ':taskId', element: lazyPage(<TaskDetailPage />) }, { path: ':taskId/edit', element: <RequireRole roles={staffRoles} />, children: [{ index: true, element: lazyPage(<TaskFormPage />) }] }] },
          { path: '/focus', element: <RequireRole roles={['STUDENT']} />, children: [{ index: true, element: lazyPage(<FocusModePage />) }] },
          { path: '/marketplace', element: <RequireRole roles={allRoles} />, children: [{ index: true, element: lazyPage(<MarketplacePage />) }] },
          { path: '/students', element: <RequireRole roles={staffRoles} />, children: [{ index: true, element: lazyPage(<StudentsPage />) }, { path: ':studentId', element: lazyPage(<StudentDetailPage />) }] },
          { path: '/schedule', element: <RequireRole roles={allRoles} />, children: [{ index: true, element: lazyPage(<SchedulePage />) }] },
          { path: '/requests', element: <RequireRole roles={allRoles} />, children: [{ index: true, element: lazyPage(<RequestsPage />) }] },
          { path: '/reviews', element: <RequireRole roles={reviewRoles} />, children: [{ index: true, element: lazyPage(<ReviewsPage />) }] },
          { path: '/files', element: <RequireRole roles={allRoles} />, children: [{ index: true, element: lazyPage(<FilesPage />) }] },
          { path: '/announcements', element: <RequireRole roles={allRoles} />, children: [{ index: true, element: lazyPage(<AnnouncementsPage />) }, { path: ':announcementId', element: lazyPage(<AnnouncementDetailPage />) }] },
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
