import { describe, expect, it } from 'vitest'
import { queryKeys } from '../../lib/query'
import { getRealtimeInvalidations } from './realtimeInvalidation'

describe('getRealtimeInvalidations', () => {
  it('targets notification and task caches for task notifications', () => {
    const invalidations = getRealtimeInvalidations({
      eventType: 'notification.created',
      notificationId: 'notification-1',
      notificationType: 'NUDGE',
      relatedEntityType: 'Task',
      relatedEntityId: 'task-1',
    }).map((item) => item.queryKey)

    expect(invalidations).toContainEqual(queryKeys.notifications.list())
    expect(invalidations).toContainEqual(queryKeys.notifications.unreadCount())
    expect(invalidations).toContainEqual(queryKeys.tasks.all)
    expect(invalidations).toContainEqual(queryKeys.tasks.detail('task-1'))
    expect(invalidations).toContainEqual(queryKeys.dashboard.all)
  })

  it('targets review and submission caches for review result notifications', () => {
    const invalidations = getRealtimeInvalidations({
      eventType: 'notification.created',
      notificationType: 'REVIEW_RESULT',
      relatedEntityType: 'Task',
      relatedEntityId: 'task-2',
    }).map((item) => item.queryKey)

    expect(invalidations).toContainEqual(queryKeys.reviews.all)
    expect(invalidations).toContainEqual(queryKeys.tasks.submissions('task-2'))
  })

  it('targets workload and meeting caches without invalidating the whole client', () => {
    const workload = getRealtimeInvalidations({ eventType: 'notification.created', notificationType: 'TIMESHEET_REMINDER', relatedEntityType: 'TimesheetWeek' }).map((item) => item.queryKey)
    const meeting = getRealtimeInvalidations({ eventType: 'notification.created', notificationType: 'MEETING', relatedEntityType: 'Meeting', relatedEntityId: 'meeting-1' }).map((item) => item.queryKey)

    expect(workload).toContainEqual(queryKeys.timesheets.all)
    expect(meeting).toContainEqual(queryKeys.meetings.all)
    expect(meeting).toContainEqual(queryKeys.meetings.detail('meeting-1'))
    expect(meeting).toContainEqual(queryKeys.meetings.slots('meeting-1'))
    expect([...workload, ...meeting]).not.toContainEqual([])
  })
})
