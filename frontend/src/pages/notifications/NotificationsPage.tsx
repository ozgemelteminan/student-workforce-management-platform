import { Check, RefreshCw } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Badge, Button, Card, CardContent, EmptyState, ErrorState, PageHeader, SearchInput, Skeleton } from '../../components/ui'
import { NotificationPreferencesPanel } from '../../components/layout/NotificationBell'
import { getNotificationRoute } from '../../features/notifications/notificationRouting'
import { useNotificationMutations, useNotificationPreferences, useNotifications } from '../../features/notifications/useNotificationQueries'
import { formatIstanbulDateTime } from '../../lib/date-time'

export function NotificationsPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const notifications = useNotifications({ page: 1, pageSize: 40, search })
  const preferences = useNotificationPreferences()
  const mutations = useNotificationMutations()

  return (
    <div className="space-y-5">
      <PageHeader title="Notifications" description="Review notifications and manage persisted notification preferences." secondaryActions={<Button variant="outline" iconBefore={<Check aria-hidden="true" className="h-4 w-4" />} onClick={() => mutations.readAll.mutate()} isLoading={mutations.readAll.isPending}>Read all</Button>} />
      <div className="grid gap-5 xl:grid-cols-[minmax(0,1.1fr)_minmax(22rem,0.9fr)]">
        <Card>
          <CardContent className="space-y-4">
            <SearchInput label="Search notifications" placeholder="Search notifications" value={search} onChange={(event) => setSearch(event.target.value)} onClear={() => setSearch('')} />
            {notifications.isError ? <ErrorState title="Notifications could not be loaded." description="Refresh the workspace or try again later." retryAction={<Button variant="outline" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={() => void notifications.refetch()}>Retry</Button>} /> : null}
            {notifications.isLoading ? <div className="space-y-2"><Skeleton className="h-20" /><Skeleton className="h-20" /></div> : null}
            {!notifications.isLoading && !notifications.isError && !notifications.data?.items.length ? <EmptyState title="You're all caught up." description="New notifications will appear here." className="min-h-48" /> : null}
            <div className="space-y-2">
              {notifications.data?.items.map((notification) => {
                const route = getNotificationRoute(notification)
                return (
                  <article key={notification.id} className="rounded-lg border border-border bg-surface px-4 py-3">
                    <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                      <div className="min-w-0">
                        <div className="flex flex-wrap items-center gap-2">
                          {!notification.isRead ? <Badge variant="brand">Unread</Badge> : <Badge>Read</Badge>}
                          <Badge variant="info">{notification.type}</Badge>
                        </div>
                        <h2 className="mt-2 text-sm font-semibold text-text-primary">{notification.title}</h2>
                        <p className="mt-1 text-sm text-text-secondary">{notification.message}</p>
                        <p className="mt-2 text-xs text-text-muted">{formatIstanbulDateTime(notification.createdAt)}</p>
                      </div>
                      <div className="flex flex-wrap gap-2">
                        {route ? <Button variant="outline" size="sm" onClick={() => navigate(route)}>Open</Button> : null}
                        {!notification.isRead ? <Button variant="ghost" size="sm" onClick={() => mutations.markRead.mutate(notification.id)}>Mark read</Button> : null}
                      </div>
                    </div>
                  </article>
                )
              })}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent>
            <NotificationPreferencesPanel
              preferences={preferences.data}
              isLoading={preferences.isLoading}
              isError={preferences.isError}
              isSaving={mutations.updatePreference.isPending}
              onRetry={() => void preferences.refetch()}
              onSave={(preferenceType, channel, isEnabled) => mutations.updatePreference.mutate({ preferenceType, channel, isEnabled })}
            />
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
