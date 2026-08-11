import type { UserRole } from '../../lib/auth/authTypes'

export function formatRole(role: UserRole): string {
  switch (role) {
    case 'ADMIN':
      return 'Admin'
    case 'TASK_MANAGER':
      return 'Task Manager'
    case 'REVIEWER':
      return 'Reviewer'
    case 'STUDENT':
      return 'Student'
  }
}
