import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Clock, Laptop, ShieldCheck, Trash2 } from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
  Badge,
  Button,
  EmptyState,
  ErrorState,
} from '../../components/ui'
import { listSessions, revokeAllSessions, revokeSession } from '../../features/auth/api/authApi'
import { formatRole } from '../../features/auth/roleDisplay'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatIstanbulDateTime } from '../../lib/date-time'
import { queryKeys } from '../../lib/query/queryKeys'
import { appToast } from '../../lib/toast'

export function SessionsPage() {
  const auth = useAuth()
  const queryClient = useQueryClient()
  const sessionId = auth.session?.sessionId ?? null
  const sessionsQuery = useQuery({
    queryKey: queryKeys.auth.sessions(sessionId),
    queryFn: ({ signal }) => listSessions(signal),
    enabled: auth.status === 'authenticated',
    staleTime: 30_000,
  })
  const revokeMutation = useMutation({
    mutationFn: revokeSession,
    onSuccess: async (_data, revokedSessionId) => {
      if (revokedSessionId === sessionId) {
        auth.clearSession('manual')
        return
      }
      await queryClient.invalidateQueries({ queryKey: queryKeys.auth.sessions(sessionId) })
      appToast.success('Session revoked.')
    },
  })
  const revokeAllMutation = useMutation({
    mutationFn: revokeAllSessions,
    onSuccess: (response) => {
      auth.clearSession('manual')
      appToast.success(`${response.revoked} session${response.revoked === 1 ? '' : 's'} revoked.`)
    },
  })

  if (!auth.user || !auth.session) {
    return null
  }

  const sessions = sessionsQuery.data ?? []

  return (
    <div className="mx-auto max-w-5xl space-y-5">
      <div className="flex flex-col justify-between gap-3 border-b border-border pb-4 sm:flex-row sm:items-end">
        <div>
          <p className="text-xs font-semibold uppercase text-brand">Account security</p>
          <h1 className="mt-1 text-2xl font-semibold text-text-primary">Sessions</h1>
          <p className="mt-1 text-sm text-text-secondary">
            Signed in as {auth.user.displayName || auth.user.email}. {auth.user.roles.map(formatRole).join(', ')}
          </p>
        </div>
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button variant="destructive" iconBefore={<Trash2 aria-hidden="true" className="h-4 w-4" />}>
              Revoke all
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Revoke all sessions?</AlertDialogTitle>
              <AlertDialogDescription>
                This backend action revokes every active session for your account, including this session.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel className="rounded-md border border-border bg-surface px-4 py-2 text-sm">Cancel</AlertDialogCancel>
              <AlertDialogAction asChild>
                <Button variant="destructive" isLoading={revokeAllMutation.isPending} onClick={() => revokeAllMutation.mutate()}>
                  Revoke all
                </Button>
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>

      {sessionsQuery.isPending ? (
        <div className="rounded-lg border border-border bg-surface p-5 text-sm text-text-secondary">Loading sessions...</div>
      ) : sessionsQuery.isError ? (
        <ErrorState
          title="Unable to load sessions"
          description="The session list could not be loaded. Your sign-in remains active."
          retryAction={<Button variant="outline" onClick={() => void sessionsQuery.refetch()}>Try again</Button>}
        />
      ) : sessions.length === 0 ? (
        <EmptyState title="No active sessions" description="The backend did not return any active sessions for this account." />
      ) : (
        <div className="overflow-hidden rounded-lg border border-border bg-surface">
          <ul className="divide-y divide-border">
            {sessions.map((session) => {
              const isCurrent = session.id === sessionId
              return (
                <li key={session.id} className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <Laptop aria-hidden="true" className="h-4 w-4 text-text-muted" />
                      <p className="font-medium text-text-primary">{session.deviceName || 'Unnamed session'}</p>
                      {isCurrent ? <Badge variant="info">Current session</Badge> : null}
                    </div>
                    <div className="mt-2 grid gap-1 text-sm text-text-secondary sm:grid-cols-2">
                      <span className="flex items-center gap-1.5">
                        <Clock aria-hidden="true" className="h-3.5 w-3.5" />
                        Created {formatIstanbulDateTime(session.createdAt)}
                      </span>
                      <span className="flex items-center gap-1.5">
                        <ShieldCheck aria-hidden="true" className="h-3.5 w-3.5" />
                        Expires {formatIstanbulDateTime(session.expiresAt)}
                      </span>
                    </div>
                  </div>
                  <AlertDialog>
                    <AlertDialogTrigger asChild>
                      <Button variant={isCurrent ? 'destructive' : 'outline'} size="sm" iconBefore={<Trash2 aria-hidden="true" className="h-4 w-4" />}>
                        Revoke
                      </Button>
                    </AlertDialogTrigger>
                    <AlertDialogContent>
                      <AlertDialogHeader>
                        <AlertDialogTitle>{isCurrent ? 'Revoke this session?' : 'Revoke session?'}</AlertDialogTitle>
                        <AlertDialogDescription>
                          {isCurrent
                            ? 'Revoking this session signs you out on this device.'
                            : 'This will revoke the selected active session.'}
                        </AlertDialogDescription>
                      </AlertDialogHeader>
                      <AlertDialogFooter>
                        <AlertDialogCancel className="rounded-md border border-border bg-surface px-4 py-2 text-sm">Cancel</AlertDialogCancel>
                        <AlertDialogAction asChild>
                          <Button
                            variant="destructive"
                            isLoading={revokeMutation.isPending}
                            onClick={() => revokeMutation.mutate(session.id)}
                          >
                            Revoke
                          </Button>
                        </AlertDialogAction>
                      </AlertDialogFooter>
                    </AlertDialogContent>
                  </AlertDialog>
                </li>
              )
            })}
          </ul>
        </div>
      )}
    </div>
  )
}
