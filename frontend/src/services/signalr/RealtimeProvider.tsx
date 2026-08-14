import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { useEffect, type ReactNode } from 'react'
import { useAuth } from '../../lib/auth/AuthProvider'
import { env } from '../../lib/env'
import { notificationsHubPath } from './notificationsHub'
import { invalidateRealtimeEvent, type RealtimeEvent } from './realtimeInvalidation'

type RealtimeProviderProps = {
  children: ReactNode
  createConnection?: (accessToken: string) => HubConnection
}

export function RealtimeProvider({ children, createConnection = createNotificationsHubConnection }: RealtimeProviderProps) {
  const queryClient = useQueryClient()
  const { status, accessToken, session } = useAuth()

  useEffect(() => {
    if (status !== 'authenticated' || !accessToken || !session?.sessionId) return undefined

    const connection = createConnection(accessToken)
    const onNotificationCreated = (event: RealtimeEvent) => {
      void invalidateRealtimeEvent(queryClient, event)
    }

    connection.on('NotificationCreated', onNotificationCreated)
    connection.onreconnected(() => {
      void invalidateRealtimeEvent(queryClient, { eventType: 'realtime.reconnected' })
    })

    void connection.start().catch(() => undefined)

    return () => {
      connection.off('NotificationCreated', onNotificationCreated)
      if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop().catch(() => undefined)
      }
    }
  }, [accessToken, createConnection, queryClient, session?.sessionId, status])

  return children
}

export function createNotificationsHubConnection(accessToken: string) {
  return new HubConnectionBuilder()
    .withUrl(`${env.apiBaseUrl}${notificationsHubPath}`, {
      accessTokenFactory: () => accessToken,
    })
    .withAutomaticReconnect([0, 2_000, 10_000, 30_000])
    .configureLogging(LogLevel.None)
    .build()
}
