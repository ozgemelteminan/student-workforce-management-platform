import type { Notification } from './types'

export function getNotificationRoute(notification: Notification): string | null {
  if (!notification.relatedEntityType || !notification.relatedEntityId) {
    return null
  }

  const entityType = normalizeEntityType(notification.relatedEntityType)
  switch (entityType) {
    case 'task':
      return `/tasks/${notification.relatedEntityId}`
    case 'student':
      return `/students/${notification.relatedEntityId}`
    case 'announcement':
      return `/announcements/${notification.relatedEntityId}`
    case 'meeting':
      return '/meetings'
    case 'timesheet':
      return '/workload'
    default:
      return null
  }
}

function normalizeEntityType(value: string): string {
  return value.trim().toLowerCase().replace(/[^a-z]/g, '')
}
