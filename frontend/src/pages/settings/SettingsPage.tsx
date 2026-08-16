import { Bell, Plus } from 'lucide-react'
import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Button, Card, CardContent, Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, EmptyState, ErrorState, Input, PageHeader, Skeleton, Textarea, Badge, AlertDialog, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogCancel, AlertDialogAction, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../components/ui'
import { useCategories, useCategoryMutations } from '../../features/categories/useCategoryQueries'
import type { Category } from '../../features/categories/types'
import { useSemesterMutations, useSemesters } from '../../features/semesters/useSemesterQueries'
import type { Semester, SemesterPayload } from '../../features/semesters/types'
import { useSkillMutations, useSkills } from '../../features/skills/useSkillQueries'
import type { Skill } from '../../features/skills/types'

type ReferenceItem = Category | Skill
type Filter = 'all' | 'active' | 'inactive'

export function SettingsPage() {
  return (
    <div className="space-y-5">
      <PageHeader title="Settings" description="Administrative settings and reference data." secondaryActions={<Button variant="outline" iconBefore={<Bell aria-hidden="true" className="h-4 w-4" />} onClick={() => window.location.assign('/notifications')}>Notification preferences</Button>} />
      <ReferenceDataPanel />
    </div>
  )
}

function ReferenceDataPanel() {
  const categories = useCategories(true)
  const skills = useSkills(true)
  const categoryMutations = useCategoryMutations()
  const skillMutations = useSkillMutations()
  const [categoryFilter, setCategoryFilter] = useState<Filter>('all')
  const [skillFilter, setSkillFilter] = useState<Filter>('all')
  const [dialog, setDialog] = useState<{ kind: 'category' | 'skill'; item?: ReferenceItem } | null>(null)
  const [confirm, setConfirm] = useState<{ kind: 'category' | 'skill'; item: ReferenceItem; action: 'deactivate' | 'reactivate' } | null>(null)

  return (
    <div className="grid gap-4 xl:grid-cols-2">
      <ReferenceSection
        title="Categories"
        description="Task types used by task creation and reporting."
        items={categories.data ?? []}
        filter={categoryFilter}
        onFilterChange={setCategoryFilter}
        isLoading={categories.isLoading}
        isError={categories.isError}
        emptyText="No categories have been created yet."
        onRetry={() => void categories.refetch()}
        onAdd={() => setDialog({ kind: 'category' })}
        onEdit={(item) => setDialog({ kind: 'category', item })}
        onToggle={(item) => setConfirm({ kind: 'category', item, action: item.isActive ? 'deactivate' : 'reactivate' })}
      />
      <ReferenceSection
        title="Skills"
        description="Capabilities used by task requirements and student profiles."
        items={skills.data ?? []}
        filter={skillFilter}
        onFilterChange={setSkillFilter}
        isLoading={skills.isLoading}
        isError={skills.isError}
        emptyText="No skills have been created yet."
        onRetry={() => void skills.refetch()}
        onAdd={() => setDialog({ kind: 'skill' })}
        onEdit={(item) => setDialog({ kind: 'skill', item })}
        onToggle={(item) => setConfirm({ kind: 'skill', item, action: item.isActive ? 'deactivate' : 'reactivate' })}
      />
      <ReferenceDialog
        state={dialog}
        pending={categoryMutations.create.isPending || categoryMutations.update.isPending || skillMutations.create.isPending || skillMutations.update.isPending}
        onClose={() => setDialog(null)}
        onSubmit={async (values) => {
          if (!dialog) return
          if (dialog.kind === 'category') {
            if (dialog.item) await categoryMutations.update.mutateAsync({ id: dialog.item.id, payload: values })
            else await categoryMutations.create.mutateAsync(values)
          } else if (dialog.item) await skillMutations.update.mutateAsync({ id: dialog.item.id, payload: values })
          else await skillMutations.create.mutateAsync(values)
          setDialog(null)
        }}
      />
      <ReferenceConfirmDialog
        state={confirm}
        pending={categoryMutations.deactivate.isPending || categoryMutations.reactivate.isPending || skillMutations.deactivate.isPending || skillMutations.reactivate.isPending}
        onClose={() => setConfirm(null)}
        onConfirm={async () => {
          if (!confirm) return
          if (confirm.kind === 'category') {
            if (confirm.action === 'deactivate') await categoryMutations.deactivate.mutateAsync(confirm.item.id)
            else await categoryMutations.reactivate.mutateAsync(confirm.item.id)
          } else if (confirm.action === 'deactivate') await skillMutations.deactivate.mutateAsync(confirm.item.id)
          else await skillMutations.reactivate.mutateAsync(confirm.item.id)
          setConfirm(null)
        }}
      />
      <SemesterSection />
    </div>
  )
}

function SemesterSection() {
  const semesters = useSemesters(true)
  const mutations = useSemesterMutations()
  const [filter, setFilter] = useState<Filter>('all')
  const [dialog, setDialog] = useState<Semester | null | 'new'>(null)
  const [confirm, setConfirm] = useState<{ item: Semester; action: 'deactivate' | 'reactivate' } | null>(null)
  const filtered = useMemo(() => (semesters.data ?? []).filter((item) => filter === 'all' || (filter === 'active' ? item.isActive : !item.isActive)), [filter, semesters.data])
  return (
    <Card className="xl:col-span-2">
      <CardContent className="space-y-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h2 className="text-base font-semibold text-text-primary">Semesters</h2>
            <p className="text-sm text-text-secondary">Academic periods available for new task and schedule selections.</p>
          </div>
          <Button size="sm" iconBefore={<Plus aria-hidden="true" className="h-4 w-4" />} onClick={() => setDialog('new')}>Add Semester</Button>
        </div>
        <div className="flex gap-1">
          {(['all', 'active', 'inactive'] as Filter[]).map((value) => <Button key={value} size="sm" variant={filter === value ? 'primary' : 'outline'} onClick={() => setFilter(value)}>{value.charAt(0).toUpperCase() + value.slice(1)}</Button>)}
        </div>
        {semesters.isError ? <ErrorState title="Semesters could not be loaded." description="Refresh or check administrator access." retryAction={<Button variant="outline" onClick={() => void semesters.refetch()}>Retry</Button>} /> : null}
        {semesters.isLoading ? <div className="space-y-2"><Skeleton className="h-12" /><Skeleton className="h-12" /></div> : null}
        {!semesters.isLoading && !semesters.isError && filtered.length === 0 ? <EmptyState title="No semesters have been created yet." className="min-h-40" /> : null}
        <div className="divide-y divide-border overflow-hidden rounded-lg border border-border">
          {filtered.map((item) => (
            <div key={item.id} className="grid gap-3 bg-surface px-4 py-3 sm:grid-cols-[1fr_auto] sm:items-center">
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="truncate font-medium text-text-primary">{item.name}</p>
                  <Badge variant={item.isActive ? 'success' : 'neutral'}>{item.isActive ? 'Active' : 'Inactive'}</Badge>
                  <Badge variant={item.status === 'ACTIVE' ? 'brand' : 'neutral'}>{item.status}</Badge>
                </div>
                <p className="mt-1 text-sm text-text-secondary">{item.startDate} to {item.endDate}</p>
              </div>
              <div className="flex gap-2">
                <Button size="sm" variant="outline" onClick={() => setDialog(item)}>Edit</Button>
                <Button size="sm" variant={item.isActive ? 'destructive' : 'outline'} onClick={() => setConfirm({ item, action: item.isActive ? 'deactivate' : 'reactivate' })}>{item.isActive ? 'Deactivate' : 'Reactivate'}</Button>
              </div>
            </div>
          ))}
        </div>
      </CardContent>
      <SemesterDialog state={dialog} pending={mutations.create.isPending || mutations.update.isPending} onClose={() => setDialog(null)} onSubmit={async (payload) => { if (dialog && dialog !== 'new') await mutations.update.mutateAsync({ id: dialog.id, payload }); else await mutations.create.mutateAsync(payload); setDialog(null) }} />
      <AlertDialog open={Boolean(confirm)} onOpenChange={(open) => !open && setConfirm(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{confirm ? `${confirm.action === 'deactivate' ? 'Deactivate' : 'Reactivate'} "${confirm.item.name}"?` : 'Update semester?'}</AlertDialogTitle>
            <AlertDialogDescription>{confirm?.action === 'deactivate' ? 'This semester will no longer be available for new selections. Existing records remain unchanged.' : 'This semester will be available for new selections again.'}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter><AlertDialogCancel>Cancel</AlertDialogCancel><AlertDialogAction asChild><Button variant={confirm?.action === 'deactivate' ? 'destructive' : 'primary'} isLoading={mutations.deactivate.isPending || mutations.reactivate.isPending} onClick={async () => { if (!confirm) return; if (confirm.action === 'deactivate') await mutations.deactivate.mutateAsync(confirm.item.id); else await mutations.reactivate.mutateAsync(confirm.item.id); setConfirm(null) }}>{confirm?.action === 'deactivate' ? 'Deactivate' : 'Reactivate'}</Button></AlertDialogAction></AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </Card>
  )
}

function SemesterDialog({ state, pending, onClose, onSubmit }: { state: Semester | 'new' | null; pending: boolean; onClose: () => void; onSubmit: (values: SemesterPayload) => Promise<void> }) {
  const [form, setForm] = useState<SemesterPayload>({ name: '', startDate: '', endDate: '', status: 'PLANNED' })
  useEffect(() => {
    setForm(state && state !== 'new' ? { name: state.name, startDate: state.startDate, endDate: state.endDate, status: state.status } : { name: '', startDate: '', endDate: '', status: 'PLANNED' })
  }, [state])
  return (
    <Dialog open={Boolean(state)} onOpenChange={(open) => { if (!open) onClose() }}>
      <DialogContent>
        <DialogHeader><DialogTitle>{state && state !== 'new' ? 'Edit Semester' : 'Add Semester'}</DialogTitle></DialogHeader>
        <form className="space-y-3" onSubmit={async (event) => { event.preventDefault(); await onSubmit(form) }}>
          <label className="space-y-1.5 text-sm font-medium text-text-primary">Name<Input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} required maxLength={120} /></label>
          <div className="grid gap-3 sm:grid-cols-2">
            <label className="space-y-1.5 text-sm font-medium text-text-primary">Start date<Input type="date" value={form.startDate} onChange={(event) => setForm({ ...form, startDate: event.target.value })} required /></label>
            <label className="space-y-1.5 text-sm font-medium text-text-primary">End date<Input type="date" value={form.endDate} onChange={(event) => setForm({ ...form, endDate: event.target.value })} required /></label>
          </div>
          <label className="space-y-1.5 text-sm font-medium text-text-primary">Status<Select value={form.status} onValueChange={(value) => setForm({ ...form, status: value as SemesterPayload['status'] })}><SelectTrigger><SelectValue /></SelectTrigger><SelectContent>{['PLANNED', 'ACTIVE', 'ARCHIVED'].map((value) => <SelectItem key={value} value={value}>{value}</SelectItem>)}</SelectContent></Select></label>
          <DialogFooter><Button type="button" variant="outline" onClick={onClose}>Cancel</Button><Button type="submit" isLoading={pending}>{state && state !== 'new' ? 'Save' : 'Add Semester'}</Button></DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function ReferenceSection(props: {
  title: string
  description: string
  items: ReferenceItem[]
  filter: Filter
  onFilterChange: (filter: Filter) => void
  isLoading: boolean
  isError: boolean
  emptyText: string
  onRetry: () => void
  onAdd: () => void
  onEdit: (item: ReferenceItem) => void
  onToggle: (item: ReferenceItem) => void
}) {
  const filtered = useMemo(() => props.items.filter((item) => props.filter === 'all' || (props.filter === 'active' ? item.isActive : !item.isActive)), [props.filter, props.items])
  return (
    <Card>
      <CardContent className="space-y-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h2 className="text-base font-semibold text-text-primary">{props.title}</h2>
            <p className="text-sm text-text-secondary">{props.description}</p>
          </div>
          <Button size="sm" iconBefore={<Plus aria-hidden="true" className="h-4 w-4" />} onClick={props.onAdd}>Add {props.title === 'Skills' ? 'Skill' : 'Category'}</Button>
        </div>
        <div className="flex gap-1">
          {(['all', 'active', 'inactive'] as Filter[]).map((filter) => <Button key={filter} size="sm" variant={props.filter === filter ? 'primary' : 'outline'} onClick={() => props.onFilterChange(filter)}>{filter.charAt(0).toUpperCase() + filter.slice(1)}</Button>)}
        </div>
        {props.isError ? <ErrorState title={`${props.title} could not be loaded.`} description="Refresh or check administrator access." retryAction={<Button variant="outline" onClick={props.onRetry}>Retry</Button>} /> : null}
        {props.isLoading ? <div className="space-y-2"><Skeleton className="h-12" /><Skeleton className="h-12" /><Skeleton className="h-12" /></div> : null}
        {!props.isLoading && !props.isError && filtered.length === 0 ? <EmptyState title={props.emptyText} className="min-h-40" /> : null}
        <div className="divide-y divide-border overflow-hidden rounded-lg border border-border">
          {filtered.map((item) => (
            <div key={item.id} className="grid gap-3 bg-surface px-4 py-3 sm:grid-cols-[1fr_auto] sm:items-center">
              <div className="min-w-0">
                <div className="flex items-center gap-2">
                  <p className="truncate font-medium text-text-primary">{item.name}</p>
                  <Badge variant={item.isActive ? 'success' : 'neutral'}>{item.isActive ? 'Active' : 'Inactive'}</Badge>
                </div>
                {item.description ? <p className="mt-1 text-sm text-text-secondary">{item.description}</p> : null}
              </div>
              <div className="flex gap-2">
                <Button size="sm" variant="outline" onClick={() => props.onEdit(item)}>Edit</Button>
                <Button size="sm" variant={item.isActive ? 'destructive' : 'outline'} onClick={() => props.onToggle(item)}>{item.isActive ? 'Deactivate' : 'Reactivate'}</Button>
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  )
}

function ReferenceDialog({ state, pending, onClose, onSubmit }: { state: { kind: 'category' | 'skill'; item?: ReferenceItem } | null; pending: boolean; onClose: () => void; onSubmit: (values: { name: string; description?: string | null }) => Promise<void> }) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  useEffect(() => {
    setName(state?.item?.name ?? '')
    setDescription(state?.item?.description ?? '')
  }, [state])
  const label = state?.kind === 'skill' ? 'Skill' : 'Category'
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    await onSubmit({ name, description: description || null })
    setName('')
    setDescription('')
  }
  return (
    <Dialog open={Boolean(state)} onOpenChange={(open) => { if (!open) { setName(''); setDescription(''); onClose() } }}>
      <DialogContent>
        <DialogHeader><DialogTitle>{state?.item ? `Edit ${label}` : `Add ${label}`}</DialogTitle></DialogHeader>
        <form className="space-y-3" onSubmit={submit}>
          <label className="space-y-1.5 text-sm font-medium text-text-primary">Name<Input value={name} onChange={(event) => setName(event.target.value)} required maxLength={120} /></label>
          <label className="space-y-1.5 text-sm font-medium text-text-primary">Description<Textarea value={description} onChange={(event) => setDescription(event.target.value)} maxLength={1000} /></label>
          <DialogFooter><Button type="button" variant="outline" onClick={onClose}>Cancel</Button><Button type="submit" isLoading={pending}>{state?.item ? 'Save' : `Add ${label}`}</Button></DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function ReferenceConfirmDialog({ state, pending, onClose, onConfirm }: { state: { kind: 'category' | 'skill'; item: ReferenceItem; action: 'deactivate' | 'reactivate' } | null; pending: boolean; onClose: () => void; onConfirm: () => Promise<void> }) {
  const noun = state?.kind === 'skill' ? 'skill' : 'category'
  return (
    <AlertDialog open={Boolean(state)} onOpenChange={(open) => !open && onClose()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{state ? `${state.action === 'deactivate' ? 'Deactivate' : 'Reactivate'} "${state.item.name}"?` : 'Update reference data?'}</AlertDialogTitle>
          <AlertDialogDescription>{state?.action === 'deactivate' ? `This ${noun} will no longer be available for new selections. Existing records remain unchanged.` : `This ${noun} will be available for new selections again.`}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter><AlertDialogCancel>Cancel</AlertDialogCancel><AlertDialogAction asChild><Button variant={state?.action === 'deactivate' ? 'destructive' : 'primary'} isLoading={pending} onClick={onConfirm}>{state?.action === 'deactivate' ? 'Deactivate' : 'Reactivate'}</Button></AlertDialogAction></AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
