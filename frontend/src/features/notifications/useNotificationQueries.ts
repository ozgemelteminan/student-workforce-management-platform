import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { getNotifications, getUnreadNotificationCount, markAllNotificationsRead, markNotificationRead } from './api/notificationsApi'
import type { NotificationFilters } from './types'

export function useUnreadNotificationCount() {
  return useQuery({
    queryKey: queryKeys.notifications.unreadCount(),
    queryFn: ({ signal }) => getUnreadNotificationCount(signal),
    staleTime: 60_000,
  })
}

export function useNotifications(filters: NotificationFilters, enabled = true) {
  return useQuery({
    queryKey: queryKeys.notifications.list(stableFilters(filters)),
    queryFn: ({ signal }) => getNotifications(filters, signal),
    enabled,
  })
}

export function useNotificationMutations() {
  const queryClient = useQueryClient()
  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications.unreadCount() }),
    ])
  }

  return {
    markRead: useMutation({
      mutationFn: (id: string) => markNotificationRead(id),
      onSuccess: async () => invalidate(),
    }),
    readAll: useMutation({
      mutationFn: () => markAllNotificationsRead(),
      onSuccess: async () => {
        appToast.success('Notifications marked as read.')
        await invalidate()
      },
    }),
  }
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
