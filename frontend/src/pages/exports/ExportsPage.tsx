import { zodResolver } from '@hookform/resolvers/zod'
import { Download, FileDown, Plus, RefreshCw } from 'lucide-react'
import { useState } from 'react'
import { type SubmitHandler, useForm } from 'react-hook-form'
import { z } from 'zod'
import { Badge, Button, Card, CardContent, Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, EmptyState, ErrorState, Input, PageHeader, SearchInput, Skeleton } from '../../components/ui'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatIstanbulDateTime } from '../../lib/date-time'
import { formatFileSize } from '../../features/files/filePresentation'
import { isProcessingStatus, useExport, useExportMutations, useExports } from '../../features/exports/useExportQueries'
import type { ExportFormat, ExportRequest, ExportType } from '../../features/exports/types'

const exportTypes: ExportType[] = ['Tasks', 'Workload', 'Students', 'Semester', 'PersonalData']
const personalExportTypes: ExportType[] = ['PersonalData']
const formats: ExportFormat[] = ['Csv', 'Xlsx', 'Pdf']

const schema = z.object({
  type: z.enum(['Tasks', 'Workload', 'Students', 'Semester', 'PersonalData']),
  format: z.enum(['Csv', 'Xlsx', 'Pdf']),
  scopeId: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

export function ExportsPage() {
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('ADMIN') ?? false
  const [search, setSearch] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const [selectedId, setSelectedId] = useState<string | undefined>()
  const exportsQuery = useExports({ page: 1, pageSize: 40, search })
  const selected = useExport(selectedId)
  const mutations = useExportMutations()

  return (
    <div className="space-y-5">
      <PageHeader title="Exports" description="Request background exports and download completed artifacts." primaryAction={<Button iconBefore={<Plus aria-hidden="true" className="h-4 w-4" />} onClick={() => setCreateOpen(true)}>New export</Button>} />
      <Card>
        <CardContent className="space-y-4">
          <SearchInput label="Search exports" placeholder="Search exports" value={search} onChange={(event) => setSearch(event.target.value)} onClear={() => setSearch('')} className="md:w-80" />
          {exportsQuery.isError ? <ErrorState title="Exports could not be loaded." description="Refresh or check your access." retryAction={<Button variant="outline" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={() => void exportsQuery.refetch()}>Retry</Button>} /> : null}
          {exportsQuery.isLoading ? <div className="space-y-2"><Skeleton className="h-20" /><Skeleton className="h-20" /></div> : null}
          {!exportsQuery.isLoading && !exportsQuery.isError && !exportsQuery.data?.items.length ? <EmptyState icon={<FileDown className="h-5 w-5" />} title="No exports requested." description="Create an export when you need an API-generated report." className="min-h-48" /> : null}
          <div className="divide-y divide-border overflow-hidden rounded-lg border border-border">
            {exportsQuery.data?.items.map((item) => (
              <div key={item.id} className="grid gap-3 bg-surface px-4 py-3 lg:grid-cols-[1fr_auto] lg:items-center">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2"><Status status={item.status} /><Badge>{item.exportType}</Badge><Badge>{item.format}</Badge></div>
                  <p className="mt-1 text-sm text-text-secondary">Requested {formatIstanbulDateTime(item.requestedAt)}{item.artifactFileSize ? ` · ${formatFileSize(item.artifactFileSize)}` : ''}</p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Button variant="outline" size="sm" onClick={() => setSelectedId(item.id)}>Details</Button>
                  <Button variant="ghost" size="sm" iconBefore={<Download aria-hidden="true" className="h-4 w-4" />} disabled={item.status !== 'COMPLETED'} isLoading={mutations.download.isPending} onClick={() => mutations.download.mutate(item.id)}>Download</Button>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
      <CreateExportDialog open={createOpen} isAdmin={isAdmin} pending={mutations.create.isPending} onClose={() => setCreateOpen(false)} onSubmit={async (values) => { await mutations.create.mutateAsync({ ...values, scopeId: values.scopeId || undefined }); setCreateOpen(false); void exportsQuery.refetch() }} />
      <Dialog open={Boolean(selectedId)} onOpenChange={(open) => !open && setSelectedId(undefined)}>
        <DialogContent>
          <DialogHeader><DialogTitle>Export detail</DialogTitle><DialogDescription>Active exports poll only this detail request until a terminal status is reached.</DialogDescription></DialogHeader>
          {selected.isLoading ? <Skeleton className="h-56" /> : selected.data ? <ExportDetail item={selected.data} downloading={mutations.download.isPending} onDownload={() => mutations.download.mutate(selected.data.id)} /> : <EmptyState title="No export detail loaded." />}
        </DialogContent>
      </Dialog>
    </div>
  )
}

function CreateExportDialog({ open, isAdmin, pending, onClose, onSubmit }: { open: boolean; isAdmin: boolean; pending: boolean; onClose: () => void; onSubmit: (values: FormValues) => Promise<void> }) {
  const allowedTypes = isAdmin ? exportTypes : personalExportTypes
  const defaultType: ExportType = isAdmin ? 'Tasks' : 'PersonalData'
  const submit: SubmitHandler<FormValues> = (values) => void onSubmit(values)
  const form = useForm<FormValues>({ resolver: zodResolver(schema), values: { type: defaultType, format: 'Csv', scopeId: '' } })
  return (
    <Dialog open={open} onOpenChange={(nextOpen) => !nextOpen && onClose()}>
      <DialogContent>
        <DialogHeader><DialogTitle>New export</DialogTitle><DialogDescription>Only export types and formats provided by the API are available.</DialogDescription></DialogHeader>
        <form className="space-y-4" onSubmit={form.handleSubmit(submit)}>
          <label className="space-y-1.5 text-sm font-medium text-text-primary">Type<select className="h-9 w-full rounded-md border border-border bg-surface px-3 text-sm" {...form.register('type')}>{allowedTypes.map((type) => <option key={type} value={type}>{type}</option>)}</select></label>
          <label className="space-y-1.5 text-sm font-medium text-text-primary">Format<select className="h-9 w-full rounded-md border border-border bg-surface px-3 text-sm" {...form.register('format')}>{formats.map((format) => <option key={format} value={format}>{format}</option>)}</select></label>
          {form.watch('type') === 'Semester' ? <label className="space-y-1.5 text-sm font-medium text-text-primary">Scope ID<Input {...form.register('scopeId')} /></label> : null}
          <DialogFooter><Button variant="outline" onClick={onClose}>Cancel</Button><Button type="submit" isLoading={pending}>Request export</Button></DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function ExportDetail({ item, downloading, onDownload }: { item: ExportRequest; downloading: boolean; onDownload: () => void }) {
  return (
    <div className="space-y-3">
      <div className="flex flex-wrap gap-2"><Status status={item.status} /><Badge>{item.exportType}</Badge><Badge>{item.format}</Badge></div>
      <dl className="grid gap-3 text-sm md:grid-cols-2">
        <Field label="Requested" value={formatIstanbulDateTime(item.requestedAt)} />
        <Field label="Processing started" value={item.processingStartedAt ? formatIstanbulDateTime(item.processingStartedAt) : 'Not started'} />
        <Field label="Completed" value={item.completedAt ? formatIstanbulDateTime(item.completedAt) : 'Not completed'} />
        <Field label="Expires" value={item.expiresAt ? formatIstanbulDateTime(item.expiresAt) : 'No expiry returned'} />
        <Field label="Artifact" value={item.artifactFileName ?? 'Not available'} />
        <Field label="Size" value={item.artifactFileSize ? formatFileSize(item.artifactFileSize) : 'Not available'} />
      </dl>
      {item.failureReason ? <p className="rounded-md border border-destructive/20 bg-destructive/5 p-3 text-sm text-destructive">{item.failureReason}</p> : null}
      {isProcessingStatus(item.status) ? <p className="text-sm text-text-secondary">This export is still being processed.</p> : null}
      <DialogFooter><Button variant="outline" onClick={onDownload} disabled={item.status !== 'COMPLETED'} isLoading={downloading} iconBefore={<Download aria-hidden="true" className="h-4 w-4" />}>Download</Button></DialogFooter>
    </div>
  )
}

function Status({ status }: { status: ExportRequest['status'] }) {
  const variant = status === 'COMPLETED' ? 'success' : status === 'FAILED' || status === 'EXPIRED' ? 'danger' : status === 'PROCESSING' ? 'info' : 'neutral'
  return <Badge variant={variant}>{status}</Badge>
}

function Field({ label, value }: { label: string; value: string }) {
  return <div className="rounded-md border border-border bg-surface-secondary p-3"><dt className="text-xs text-text-muted">{label}</dt><dd className="mt-1 break-words font-medium text-text-primary">{value}</dd></div>
}
