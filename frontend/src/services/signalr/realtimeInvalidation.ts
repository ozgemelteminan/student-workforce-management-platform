import type { QueryClient, QueryKey } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'

export type RealtimeEvent = {
  eventType?: string
  notificationId?: string
  notificationType?: string
  relatedEntityType?: string | null
  relatedEntityId?: string | null
}

type QueryInvalidation = {
  queryKey: QueryKey
}

export function getRealtimeInvalidations(event: RealtimeEvent): QueryInvalidation[] {
  const invalidations: QueryInvalidation[] = []

  if (event.eventType === 'realtime.reconnected' || event.eventType === 'notification.created' || event.notificationId) {
    invalidations.push({ queryKey: queryKeys.notifications.list() })
    invalidations.push({ queryKey: queryKeys.notifications.unreadCount() })
  }

  const notificationType = normalize(event.notificationType)
  const entityType = normalize(event.relatedEntityType)
  const entityId = event.relatedEntityId ?? undefined

  if (entityType === 'task' || notificationType === 'task_assigned' || notificationType === 'deadline_reminder' || notificationType === 'overdue' || notificationType === 'nudge') {
    invalidations.push({ queryKey: queryKeys.tasks.all })
    invalidations.push({ queryKey: queryKeys.dashboard.all })
    if (entityId) invalidations.push({ queryKey: queryKeys.tasks.detail(entityId) })
  }

  if (notificationType === 'request_result') {
    invalidations.push({ queryKey: queryKeys.requests.all })
    if (entityType === 'task' && entityId) invalidations.push({ queryKey: queryKeys.tasks.detail(entityId) })
  }

  if (notificationType === 'review_result') {
    invalidations.push({ queryKey: queryKeys.reviews.all })
    if (entityType === 'task' && entityId) {
      invalidations.push({ queryKey: queryKeys.tasks.detail(entityId) })
      invalidations.push({ queryKey: queryKeys.tasks.submissions(entityId) })
    }
  }

  if (entityType === 'meeting' || notificationType === 'meeting') {
    invalidations.push({ queryKey: queryKeys.meetings.all })
    if (entityId) {
      invalidations.push({ queryKey: queryKeys.meetings.detail(entityId) })
      invalidations.push({ queryKey: queryKeys.meetings.slots(entityId) })
    }
  }

  if (entityType === 'timesheetweek' || notificationType === 'timesheet_reminder') {
    invalidations.push({ queryKey: queryKeys.timesheets.all })
  }

  if (entityType === 'announcement' || notificationType === 'announcement') {
    invalidations.push({ queryKey: queryKeys.announcements.all })
    if (entityId) invalidations.push({ queryKey: queryKeys.announcements.detail(entityId) })
  }

  return dedupeInvalidations(invalidations)
}

export async function invalidateRealtimeEvent(queryClient: QueryClient, event: RealtimeEvent) {
  await Promise.all(getRealtimeInvalidations(event).map((invalidation) => queryClient.invalidateQueries(invalidation)))
}

function normalize(value: string | null | undefined) {
  return value?.trim().toLowerCase().replace(/[^a-z0-9_]/g, '') ?? ''
}

function dedupeInvalidations(invalidations: QueryInvalidation[]) {
  const seen = new Set<string>()
  return invalidations.filter((invalidation) => {
    const key = JSON.stringify(invalidation.queryKey)
    if (seen.has(key)) return false
    seen.add(key)
    return true
  })
}
