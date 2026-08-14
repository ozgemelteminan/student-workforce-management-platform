export type NotificationType = 'TASK_ASSIGNED' | 'DEADLINE_REMINDER' | 'OVERDUE' | 'REQUEST_RESULT' | 'REVIEW_RESULT' | 'COMMENT' | 'ANNOUNCEMENT' | 'REMINDER_SENT' | 'NUDGE' | 'TIMESHEET_REMINDER' | 'MEETING'

export type Notification = {
  id: string
  userId: string
  type: NotificationType
  title: string
  message: string
  relatedEntityType: string | null
  relatedEntityId: string | null
  isRead: boolean
  createdAt: string
  readAt: string | null
}

export type PaginatedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export type NotificationFilters = {
  page?: number
  pageSize?: number
  search?: string
}

export type UnreadCountResponse = {
  count: number
}

export type NotificationPreferenceType =
  | 'TaskAssigned'
  | 'DeadlineReminder'
  | 'Overdue'
  | 'RequestResult'
  | 'ReviewResult'
  | 'Comment'
  | 'Announcement'

export type NotificationChannel = 'IN_APP' | 'EMAIL'

export type NotificationPreferenceSetting = {
  preferenceType: NotificationPreferenceType
  channel: NotificationChannel
  isEnabled: boolean
}

export type NotificationPreference = {
  id: string
  userId: string
  preferenceType: NotificationPreferenceType
  channel: NotificationChannel
  isEnabled: boolean
}

export type UpdateNotificationPreferenceRequest = {
  preferenceType: NotificationPreferenceType
  channel: NotificationChannel
  isEnabled: boolean
}
