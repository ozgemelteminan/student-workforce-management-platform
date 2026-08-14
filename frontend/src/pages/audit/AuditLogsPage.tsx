import { Eye, RefreshCw } from 'lucide-react'
import { useState } from 'react'
import { Badge, Button, Card, CardContent, Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, EmptyState, ErrorState, Input, PageHeader, SearchInput, Skeleton } from '../../components/ui'
import { formatMetadata, useAuditLog, useAuditLogs } from '../../features/audit/useAuditQueries'
import { formatIstanbulDateTime } from '../../lib/date-time'

export function AuditLogsPage() {
  const [search, setSearch] = useState('')
  const [action, setAction] = useState('')
  const [entityType, setEntityType] = useState('')
  const [selectedId, setSelectedId] = useState<string | undefined>()
  const logs = useAuditLogs({ page: 1, pageSize: 40, search, action, entityType })
  const detail = useAuditLog(selectedId)

  return (
    <div className="space-y-5">
      <PageHeader title="Audit Logs" description="Administrative audit trail from the API." />
      <Card>
        <CardContent className="space-y-4">
          <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_12rem_12rem]">
            <SearchInput label="Search audit logs" placeholder="Search" value={search} onChange={(event) => setSearch(event.target.value)} onClear={() => setSearch('')} />
            <Input aria-label="Action filter" placeholder="Action" value={action} onChange={(event) => setAction(event.target.value)} />
            <Input aria-label="Entity type filter" placeholder="Entity type" value={entityType} onChange={(event) => setEntityType(event.target.value)} />
          </div>
          {logs.isError ? <ErrorState title="Audit logs could not be loaded." description="Refresh or check administrator access." retryAction={<Button variant="outline" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={() => void logs.refetch()}>Retry</Button>} /> : null}
          {logs.isLoading ? <div className="space-y-2"><Skeleton className="h-16" /><Skeleton className="h-16" /></div> : null}
          {!logs.isLoading && !logs.isError && !logs.data?.items.length ? <EmptyState title="No audit logs matched." className="min-h-48" /> : null}
          <div className="divide-y divide-border overflow-hidden rounded-lg border border-border">
            {logs.data?.items.map((log) => (
              <div key={log.id} className="grid gap-3 bg-surface px-4 py-3 lg:grid-cols-[1fr_auto] lg:items-center">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2"><Badge variant="info">{log.action}</Badge><Badge>{log.entityType}</Badge>{log.entityId ? <span className="text-xs text-text-muted">{log.entityId}</span> : null}</div>
                  <p className="mt-1 text-sm text-text-secondary">Actor {log.userId ?? 'system'} · {formatIstanbulDateTime(log.createdAt)}</p>
                </div>
                <Button variant="outline" size="sm" iconBefore={<Eye aria-hidden="true" className="h-4 w-4" />} onClick={() => setSelectedId(log.id)}>Details</Button>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
      <Dialog open={Boolean(selectedId)} onOpenChange={(open) => !open && setSelectedId(undefined)}>
        <DialogContent>
          <DialogHeader><DialogTitle>Audit detail</DialogTitle><DialogDescription>Structured values are rendered as text, never as HTML.</DialogDescription></DialogHeader>
          {detail.isLoading ? <Skeleton className="h-64" /> : detail.data ? <pre className="max-h-[28rem] overflow-auto rounded-md border border-border bg-surface-secondary p-3 text-xs text-text-primary">{JSON.stringify({ ...detail.data, oldValue: formatMetadata(detail.data.oldValue), newValue: formatMetadata(detail.data.newValue) }, null, 2)}</pre> : <EmptyState title="No detail loaded." />}
        </DialogContent>
      </Dialog>
    </div>
  )
}
