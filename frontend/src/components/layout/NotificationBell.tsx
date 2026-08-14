import { Bell, Check, ExternalLink, Settings2 } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getNotificationRoute } from '../../features/notifications/notificationRouting'
import { useNotificationMutations, useNotificationPreferences, useNotifications, useUnreadNotificationCount } from '../../features/notifications/useNotificationQueries'
import type { Notification, NotificationChannel, NotificationPreferenceSetting, NotificationPreferenceType } from '../../features/notifications/types'
import { formatIstanbulDateTime, formatRelativeTime } from '../../lib/date-time'
import { cn } from '../../lib/utils/cn'
import { Badge } from '../ui/badge'
import { Button } from '../ui/button'
import { Checkbox } from '../ui/checkbox'
import { EmptyState } from '../ui/empty-state'
import { ErrorState } from '../ui/error-state'
import { IconButton } from '../ui/icon-button'
import { Popover, PopoverContent, PopoverTrigger } from '../ui/popover'
import { Skeleton } from '../ui/skeleton'

export function NotificationBell() {
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [showPreferences, setShowPreferences] = useState(false)
  const unreadCount = useUnreadNotificationCount()
  const notifications = useNotifications({ page: 1, pageSize: 8 }, open)
  const preferences = useNotificationPreferences(open && showPreferences)
  const mutations = useNotificationMutations()
  const count = unreadCount.data?.count ?? 0
  const accessibleLabel = count > 0 ? `Open notifications, ${count} unread` : 'Open notifications'

  const activateNotification = async (notification: Notification) => {
    if (!notification.isRead) {
      await mutations.markRead.mutateAsync(notification.id).catch(() => undefined)
    }
    const route = getNotificationRoute(notification)
    if (route) {
      setOpen(false)
      navigate(route)
    }
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <span className="relative inline-flex">
          <IconButton label={accessibleLabel} tooltip="Notifications" icon={<Bell aria-hidden="true" className="h-4 w-4" />} />
          {count > 0 ? (
            <span className="absolute -right-1 -top-1 min-w-5 rounded-full border border-surface bg-brand px-1 text-center text-[10px] font-semibold leading-5 text-text-inverse">
              {count > 99 ? '99+' : count}
            </span>
          ) : null}
        </span>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-[calc(100vw-1.5rem)] max-w-md p-0">
        <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3">
          <h2 className="text-sm font-semibold text-text-primary">Notifications</h2>
          <div className="flex gap-1">
            <Button size="sm" variant="ghost" iconBefore={<Settings2 aria-hidden="true" className="h-4 w-4" />} onClick={() => setShowPreferences((value) => !value)}>
              Preferences
            </Button>
            <Button size="sm" variant="ghost" iconBefore={<Check aria-hidden="true" className="h-4 w-4" />} disabled={!count || mutations.readAll.isPending} onClick={() => mutations.readAll.mutate()}>
              Read all
            </Button>
          </div>
        </div>
        <div className="max-h-[65vh] overflow-y-auto p-3">
          {showPreferences ? (
            <NotificationPreferencesPanel
              preferences={preferences.data}
              isLoading={preferences.isLoading}
              isError={preferences.isError}
              isSaving={mutations.updatePreference.isPending}
              onRetry={() => void preferences.refetch()}
              onSave={(preferenceType, channel, isEnabled) => mutations.updatePreference.mutate({ preferenceType, channel, isEnabled })}
            />
          ) : notifications.isError ? (
            <ErrorState title="Notifications could not be loaded." description="Refresh the panel or try again later." retryAction={<Button variant="outline" size="sm" onClick={() => void notifications.refetch()}>Retry</Button>} className="border-0 bg-surface-secondary px-4 py-6" />
          ) : notifications.isLoading ? (
            <div className="space-y-2">{Array.from({ length: 4 }).map((_, index) => <Skeleton key={index} className="h-16" />)}</div>
          ) : notifications.data?.items.length ? (
            <div className="space-y-2">
              {notifications.data.items.map((notification) => <NotificationRow key={notification.id} notification={notification} onActivate={() => void activateNotification(notification)} onMarkRead={() => mutations.markRead.mutate(notification.id)} />)}
            </div>
          ) : (
            <EmptyState title="You're all caught up." description="New notifications will appear here." className="min-h-36 border-0 bg-surface-secondary" />
          )}
        </div>
      </PopoverContent>
    </Popover>
  )
}

const preferenceLabels: Record<NotificationPreferenceType, string> = {
  TaskAssigned: 'Task assigned',
  DeadlineReminder: 'Deadline reminder',
  Overdue: 'Overdue task',
  RequestResult: 'Request result',
  ReviewResult: 'Review result',
  Comment: 'Comment',
  Announcement: 'Announcement',
}

const channelLabels: Record<NotificationChannel, string> = {
  IN_APP: 'In-app',
  EMAIL: 'Email',
}

const preferenceTypes = Object.keys(preferenceLabels) as NotificationPreferenceType[]
const channels: NotificationChannel[] = ['IN_APP', 'EMAIL']

export function NotificationPreferencesPanel({ preferences, isLoading, isError, isSaving, onRetry, onSave }: { preferences: NotificationPreferenceSetting[] | undefined; isLoading: boolean; isError: boolean; isSaving: boolean; onRetry: () => void; onSave: (preferenceType: NotificationPreferenceType, channel: NotificationChannel, isEnabled: boolean) => void }) {
  if (isError) {
    return <ErrorState title="Preferences could not be loaded." description="Refresh preferences before making changes." retryAction={<Button variant="outline" size="sm" onClick={onRetry}>Retry</Button>} className="border-0 bg-surface-secondary px-4 py-6" />
  }

  if (isLoading || !preferences) {
    return <div className="space-y-2">{Array.from({ length: 5 }).map((_, index) => <Skeleton key={index} className="h-14" />)}</div>
  }

  return (
    <div className="space-y-3" aria-label="Notification preferences">
      {preferenceTypes.map((preferenceType) => (
        <div key={preferenceType} className="rounded-lg border border-border bg-surface px-3 py-3">
          <p className="text-sm font-medium text-text-primary">{preferenceLabels[preferenceType]}</p>
          <div className="mt-3 grid gap-2 sm:grid-cols-2">
            {channels.map((channel) => {
              const preference = preferences.find((item) => item.preferenceType === preferenceType && item.channel === channel)
              return (
                <div key={channel} className="rounded-md border border-border bg-surface-secondary px-3 py-2">
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-sm font-medium text-text-primary">{channelLabels[channel]}</span>
                    <Badge variant={preference ? (preference.isEnabled ? 'success' : 'neutral') : 'warning'}>{preference ? (preference.isEnabled ? 'Enabled' : 'Disabled') : 'Not configured'}</Badge>
                  </div>
                  {preference ? (
                    <label className="mt-2 flex items-center gap-2 text-sm text-text-secondary">
                      <Checkbox
                        checked={preference.isEnabled}
                        disabled={isSaving}
                        onCheckedChange={(checked) => onSave(preference.preferenceType, preference.channel, checked === true)}
                      />
                      Enabled
                    </label>
                  ) : (
                    <div className="mt-2 flex gap-2">
                      <Button size="sm" variant="outline" disabled={isSaving} onClick={() => onSave(preferenceType, channel, true)}>Enable</Button>
                      <Button size="sm" variant="ghost" disabled={isSaving} onClick={() => onSave(preferenceType, channel, false)}>Disable</Button>
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        </div>
      ))}
    </div>
  )
}

function NotificationRow({ notification, onActivate, onMarkRead }: { notification: Notification; onActivate: () => void; onMarkRead: () => void }) {
  const route = getNotificationRoute(notification)
  return (
    <article className={cn('rounded-lg border border-border bg-surface px-3 py-3', !notification.isRead && 'border-brand/30 bg-brand-subtle/40')}>
      <div className="flex items-start gap-3">
        <span aria-hidden="true" className={cn('mt-1.5 h-2 w-2 shrink-0 rounded-full border', notification.isRead ? 'border-border bg-surface-secondary' : 'border-brand bg-brand')} />
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className={cn('min-w-0 truncate text-sm text-text-primary', !notification.isRead && 'font-semibold')}>{notification.title}</h3>
            {!notification.isRead ? <Badge variant="brand">Unread</Badge> : null}
          </div>
          <p className="mt-1 line-clamp-2 text-sm text-text-secondary">{notification.message}</p>
          <p className="mt-2 text-xs text-text-muted" title={formatIstanbulDateTime(notification.createdAt)}>{formatRelativeTime(notification.createdAt)}</p>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button size="sm" variant={route ? 'outline' : 'ghost'} iconBefore={route ? <ExternalLink aria-hidden="true" className="h-4 w-4" /> : undefined} onClick={onActivate}>{route ? 'Open' : notification.isRead ? 'Viewed' : 'Mark read'}</Button>
            {!notification.isRead && route ? <Button size="sm" variant="ghost" onClick={onMarkRead}>Mark read</Button> : null}
          </div>
        </div>
      </div>
    </article>
  )
}
