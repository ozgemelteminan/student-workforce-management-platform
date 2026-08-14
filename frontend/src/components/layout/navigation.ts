import {
  BarChart3,
  Bell,
  CalendarDays,
  ClipboardCheck,
  ClipboardList,
  FileDown,
  FileText,
  FolderOpen,
  History,
  Home,
  Megaphone,
  Repeat,
  Settings,
  Store,
  Users,
} from 'lucide-react'
import type { ComponentType, SVGProps } from 'react'
import type { UserRole } from '../../lib/auth/authTypes'

export type NavigationItem = {
  label: string
  href: string
  icon: ComponentType<SVGProps<SVGSVGElement>>
  roles: UserRole[]
}

export type NavigationGroup = {
  label: string
  items: NavigationItem[]
}

const allRoles: UserRole[] = ['ADMIN', 'TASK_MANAGER', 'REVIEWER', 'STUDENT']
const staffRoles: UserRole[] = ['ADMIN', 'TASK_MANAGER']
const reviewRoles: UserRole[] = ['ADMIN', 'REVIEWER']

export const navigationGroups: NavigationGroup[] = [
  {
    label: 'WORKSPACE',
    items: [
      { label: 'Dashboard', href: '/', icon: Home, roles: allRoles },
      { label: 'Tasks', href: '/tasks', icon: ClipboardList, roles: allRoles },
      { label: 'Marketplace', href: '/marketplace', icon: Store, roles: allRoles },
    ],
  },
  {
    label: 'WORKFORCE',
    items: [
      { label: 'Students', href: '/students', icon: Users, roles: staffRoles },
      { label: 'Schedule', href: '/schedule', icon: CalendarDays, roles: allRoles },
      { label: 'Requests', href: '/requests', icon: ClipboardCheck, roles: allRoles },
      { label: 'Reviews', href: '/reviews', icon: ClipboardCheck, roles: reviewRoles },
    ],
  },
  {
    label: 'CONTENT',
    items: [
      { label: 'Files', href: '/files', icon: FolderOpen, roles: allRoles },
      { label: 'Announcements', href: '/announcements', icon: Megaphone, roles: allRoles },
      { label: 'Templates', href: '/templates', icon: FileText, roles: staffRoles },
      { label: 'Recurring Tasks', href: '/recurring-tasks', icon: Repeat, roles: staffRoles },
      { label: 'Notifications', href: '/notifications', icon: Bell, roles: allRoles },
    ],
  },
  {
    label: 'INSIGHTS',
    items: [
      { label: 'Analytics', href: '/analytics', icon: BarChart3, roles: staffRoles },
      { label: 'Exports', href: '/exports', icon: FileDown, roles: allRoles },
    ],
  },
  {
    label: 'ADMIN',
    items: [
      { label: 'Audit Logs', href: '/audit-logs', icon: History, roles: ['ADMIN'] },
      { label: 'Settings', href: '/settings', icon: Settings, roles: ['ADMIN'] },
    ],
  },
]

export function visibleNavigationGroups(roles: readonly UserRole[] | undefined): NavigationGroup[] {
  const effectiveRoles = roles ?? []
  return navigationGroups
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => item.roles.some((role) => effectiveRoles.includes(role))),
    }))
    .filter((group) => group.items.length > 0)
}

export function flattenNavigation(groups: NavigationGroup[] = navigationGroups): NavigationItem[] {
  return groups.flatMap((group) => group.items)
}

export function findNavigationItem(pathname: string, groups: NavigationGroup[] = navigationGroups): NavigationItem | undefined {
  return flattenNavigation(groups).find((item) => item.href === pathname || (item.href !== '/' && pathname.startsWith(`${item.href}/`)))
}
