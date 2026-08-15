import { describe, expect, it } from 'vitest'
import { getNotificationRoute } from './notificationRouting'
import type { Notification } from './types'

const baseNotification: Notification = {
  id: 'notification-id',
  userId: 'user-id',
  type: 'TASK_ASSIGNED',
  title: 'Task assigned',
  message: 'A task was assigned.',
  relatedEntityType: null,
  relatedEntityId: null,
  isRead: false,
  createdAt: '2026-08-11T12:00:00Z',
  readAt: null,
}

describe('notification routing', () => {
  it('maps known structured entities to internal routes', () => {
    expect(getNotificationRoute({ ...baseNotification, relatedEntityType: 'Task', relatedEntityId: 'task-id' })).toBe('/tasks/task-id')
    expect(getNotificationRoute({ ...baseNotification, relatedEntityType: 'Announcement', relatedEntityId: 'announcement-id' })).toBe('/announcements/announcement-id')
    expect(getNotificationRoute({ ...baseNotification, relatedEntityType: 'Meeting', relatedEntityId: 'meeting-id' })).toBe('/meetings/meeting-id')
  })

  it('does not invent routes for unsupported metadata', () => {
    expect(getNotificationRoute({ ...baseNotification, relatedEntityType: 'https://example.test', relatedEntityId: 'id' })).toBeNull()
    expect(getNotificationRoute(baseNotification)).toBeNull()
  })
})
