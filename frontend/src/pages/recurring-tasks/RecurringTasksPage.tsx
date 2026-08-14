import { zodResolver } from '@hookform/resolvers/zod'
import { Pencil, Plus, Power, PowerOff, RefreshCw, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, Badge, Button, Card, CardContent, Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, EmptyState, ErrorState, Input, PageHeader, SearchInput, Skeleton } from '../../components/ui'
import { useRecurringTaskMutations, useRecurringTasks, describeRecurrence } from '../../features/recurring-tasks/useRecurringTaskQueries'
import type { RecurringTask, RecurringTaskPayload } from '../../features/recurring-tasks/types'
import { useTemplates } from '../../features/templates/useTemplateQueries'
import { formatIstanbulDateTime } from '../../lib/date-time'

const schema = z.object({
  templateId: z.string().optional(),
  frequency: z.string().trim().min(1).max(120),
  timeZoneId: z.string().trim().min(1).max(120),
  localRunTime: z.string().optional(),
  nextRunAt: z.string().trim().min(1),
})

type FormValues = z.infer<typeof schema>

export function RecurringTasksPage() {
  const [search, setSearch] = useState('')
  const [activeFilter, setActiveFilter] = useState<boolean | undefined>(undefined)
  const [editing, setEditing] = useState<RecurringTask | null>(null)
  const [deleting, setDeleting] = useState<RecurringTask | null>(null)
  const recurring = useRecurringTasks({ page: 1, pageSize: 40, search, isActive: activeFilter })
  const templates = useTemplates({ page: 1, pageSize: 100 })
  const mutations = useRecurringTaskMutations()

  return (
    <div className="space-y-5">
      <PageHeader title="Recurring Tasks" description="Schedule template-based task generation with server-owned recurrence execution." primaryAction={<Button iconBefore={<Plus aria-hidden="true" className="h-4 w-4" />} onClick={() => setEditing(emptyRecurring())}>New recurring task</Button>} />
      <Card>
        <CardContent className="space-y-4">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <SearchInput label="Search recurring tasks" placeholder="Search frequency or time zone" value={search} onChange={(event) => setSearch(event.target.value)} onClear={() => setSearch('')} className="md:w-80" />
            <div className="flex rounded-md border border-border bg-surface-secondary p-1">
              <Button size="sm" variant={activeFilter === undefined ? 'primary' : 'ghost'} onClick={() => setActiveFilter(undefined)}>All</Button>
              <Button size="sm" variant={activeFilter === true ? 'primary' : 'ghost'} onClick={() => setActiveFilter(true)}>Active</Button>
              <Button size="sm" variant={activeFilter === false ? 'primary' : 'ghost'} onClick={() => setActiveFilter(false)}>Inactive</Button>
            </div>
          </div>
          {recurring.isError ? <ErrorState title="Recurring tasks could not be loaded." description="Refresh or check your access." retryAction={<Button variant="outline" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={() => void recurring.refetch()}>Retry</Button>} /> : null}
          {recurring.isLoading ? <div className="space-y-3"><Skeleton className="h-24" /><Skeleton className="h-24" /></div> : null}
          {!recurring.isLoading && !recurring.isError && !recurring.data?.items.length ? <EmptyState title="No recurring tasks." description="Create one from an existing task template." className="min-h-48" /> : null}
          <div className="grid gap-3">
            {recurring.data?.items.map((item) => (
              <article key={item.id} className="rounded-lg border border-border bg-surface px-4 py-4">
                <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                  <div className="space-y-2">
                    <div className="flex flex-wrap gap-2"><Badge variant={item.isActive ? 'success' : 'neutral'}>{item.isActive ? 'Active' : 'Inactive'}</Badge><Badge>{item.timeZoneId}</Badge></div>
                    <h2 className="text-base font-semibold text-text-primary">{describeRecurrence(item.frequency, item.localRunTime)}</h2>
                    <p className="text-sm text-text-secondary">Template {item.templateId.slice(0, 8)} · next run {formatIstanbulDateTime(item.nextRunAt)}</p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button variant="outline" size="sm" iconBefore={item.isActive ? <PowerOff aria-hidden="true" className="h-4 w-4" /> : <Power aria-hidden="true" className="h-4 w-4" />} isLoading={mutations.activate.isPending || mutations.deactivate.isPending} onClick={() => item.isActive ? mutations.deactivate.mutate(item.id) : mutations.activate.mutate(item.id)}>{item.isActive ? 'Deactivate' : 'Activate'}</Button>
                    <Button variant="ghost" size="sm" iconBefore={<Pencil aria-hidden="true" className="h-4 w-4" />} onClick={() => setEditing(item)}>Edit</Button>
                    <Button variant="ghost" size="sm" iconBefore={<Trash2 aria-hidden="true" className="h-4 w-4" />} onClick={() => setDeleting(item)}>Delete</Button>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </CardContent>
      </Card>
      <RecurringDialog recurring={editing} templates={templates.data?.items ?? []} pending={mutations.create.isPending || mutations.update.isPending} onClose={() => setEditing(null)} onSubmit={async (payload) => {
        if (!editing) return
        if (editing.id) await mutations.update.mutateAsync({ id: editing.id, payload })
        else if (payload.templateId) await mutations.create.mutateAsync(payload as Required<RecurringTaskPayload>)
        setEditing(null)
      }} />
      <AlertDialog open={Boolean(deleting)} onOpenChange={(open) => !open && setDeleting(null)}>
        <AlertDialogContent><AlertDialogHeader><AlertDialogTitle>Delete recurring task?</AlertDialogTitle><AlertDialogDescription>This stops future schedule entries for this recurring task.</AlertDialogDescription></AlertDialogHeader><AlertDialogFooter><AlertDialogCancel>Cancel</AlertDialogCancel><AlertDialogAction onClick={() => deleting && mutations.delete.mutate(deleting.id, { onSettled: () => setDeleting(null) })}>Delete</AlertDialogAction></AlertDialogFooter></AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function RecurringDialog({ recurring, templates, pending, onClose, onSubmit }: { recurring: RecurringTask | null; templates: { id: string; title: string }[]; pending: boolean; onClose: () => void; onSubmit: (payload: RecurringTaskPayload) => Promise<void> }) {
  const form = useForm<FormValues>({ resolver: zodResolver(schema), values: { templateId: recurring?.templateId || templates[0]?.id || '', frequency: recurring?.frequency ?? '', timeZoneId: recurring?.timeZoneId ?? 'Europe/Istanbul', localRunTime: recurring?.localRunTime ?? '', nextRunAt: recurring?.nextRunAt ?? '' } })
  return (
    <Dialog open={Boolean(recurring)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader><DialogTitle>{recurring?.id ? 'Edit recurring task' : 'New recurring task'}</DialogTitle><DialogDescription>Use the exact recurrence fields supported by the API.</DialogDescription></DialogHeader>
        <form className="space-y-4" onSubmit={form.handleSubmit((values) => onSubmit({ ...values, localRunTime: values.localRunTime || undefined }))}>
          {!recurring?.id ? <Label title="Template"><select className="h-9 w-full rounded-md border border-border bg-surface px-3 text-sm" {...form.register('templateId')}>{templates.map((template) => <option key={template.id} value={template.id}>{template.title}</option>)}</select></Label> : null}
          <Label title="Frequency" error={form.formState.errors.frequency?.message}><Input placeholder="Daily" {...form.register('frequency')} /></Label>
          <Label title="Time zone" error={form.formState.errors.timeZoneId?.message}><Input {...form.register('timeZoneId')} /></Label>
          <Label title="Local run time"><Input placeholder="09:00" {...form.register('localRunTime')} /></Label>
          <Label title="Next run UTC" error={form.formState.errors.nextRunAt?.message}><Input placeholder="2026-08-14T06:00:00Z" {...form.register('nextRunAt')} /></Label>
          <DialogFooter><Button variant="outline" onClick={onClose}>Cancel</Button><Button type="submit" isLoading={pending}>Save</Button></DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function Label({ title, error, children }: { title: string; error?: string; children: React.ReactNode }) {
  return <label className="space-y-1.5 text-sm font-medium text-text-primary">{title}{children}{error ? <span className="block text-xs text-destructive">{error}</span> : null}</label>
}

function emptyRecurring(): RecurringTask {
  return { id: '', templateId: '', frequency: '', timeZoneId: 'Europe/Istanbul', nextRunAt: '', isActive: true, createdById: '', concurrencyToken: '', createdAt: new Date().toISOString() }
}
