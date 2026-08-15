import { Bell, Pencil, Plus, RefreshCw } from 'lucide-react'
import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Button, Card, CardContent, Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, EmptyState, ErrorState, Input, PageHeader, Skeleton, Tabs, TabsContent, TabsList, TabsTrigger, Textarea, Badge, AlertDialog, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogCancel, AlertDialogAction } from '../../components/ui'
import { useCategories, useCategoryMutations } from '../../features/categories/useCategoryQueries'
import type { Category } from '../../features/categories/types'
import { useSettingMutations, useSettings } from '../../features/settings/useSettingsQueries'
import type { SystemSetting } from '../../features/settings/types'
import { useSkillMutations, useSkills } from '../../features/skills/useSkillQueries'
import type { Skill } from '../../features/skills/types'

type ReferenceItem = Category | Skill
type Filter = 'all' | 'active' | 'inactive'

export function SettingsPage() {
  const settings = useSettings()
  const mutations = useSettingMutations()
  const [editing, setEditing] = useState<SystemSetting | null>(null)

  return (
    <div className="space-y-5">
      <PageHeader title="Settings" description="Administrative settings and reference data." secondaryActions={<Button variant="outline" iconBefore={<Bell aria-hidden="true" className="h-4 w-4" />} onClick={() => window.location.assign('/notifications')}>Notification preferences</Button>} />
      <Tabs defaultValue="reference-data">
        <TabsList>
          <TabsTrigger value="reference-data">Categories & Skills</TabsTrigger>
          <TabsTrigger value="system-settings">System settings</TabsTrigger>
        </TabsList>
        <TabsContent value="reference-data">
          <ReferenceDataPanel />
        </TabsContent>
        <TabsContent value="system-settings">
          <Card>
            <CardContent className="space-y-4">
              {settings.isError ? <ErrorState title="Settings could not be loaded." description="Refresh or check administrator access." retryAction={<Button variant="outline" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={() => void settings.refetch()}>Retry</Button>} /> : null}
              {settings.isLoading ? <div className="space-y-2"><Skeleton className="h-16" /><Skeleton className="h-16" /></div> : null}
              {!settings.isLoading && !settings.isError && !settings.data?.length ? <EmptyState title="No system settings are exposed." className="min-h-48" /> : null}
              <div className="divide-y divide-border overflow-hidden rounded-lg border border-border">
                {settings.data?.map((setting) => (
                  <div key={setting.id} className="grid gap-3 bg-surface px-4 py-3 lg:grid-cols-[1fr_auto] lg:items-center">
                    <div className="min-w-0">
                      <p className="font-medium text-text-primary">{setting.key}</p>
                      {setting.description ? <p className="text-sm text-text-secondary">{setting.description}</p> : null}
                      <p className="mt-1 truncate text-xs text-text-muted">Value: {setting.value}</p>
                    </div>
                    <Button variant="outline" size="sm" iconBefore={<Pencil aria-hidden="true" className="h-4 w-4" />} onClick={() => setEditing(setting)}>Edit</Button>
                  </div>
                ))}
              </div>
              <p className="text-sm text-text-secondary"><Link className="font-medium text-brand hover:text-brand-hover" to="/notifications">Notification preferences</Link> are managed in the Notifications workspace.</p>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
      <SettingDialog setting={editing} pending={mutations.update.isPending} onClose={() => setEditing(null)} onSubmit={async (value) => { if (editing) await mutations.update.mutateAsync({ key: editing.key, value, concurrencyToken: editing.concurrencyToken }); setEditing(null) }} />
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
    </div>
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

function SettingDialog({ setting, pending, onClose, onSubmit }: { setting: SystemSetting | null; pending: boolean; onClose: () => void; onSubmit: (value: string) => Promise<void> }) {
  const [value, setValue] = useState('')
  useEffect(() => {
    setValue(setting?.value ?? '')
  }, [setting])
  return (
    <Dialog open={Boolean(setting)} onOpenChange={(open) => { if (!open) { setValue(''); onClose() } }}>
      <DialogContent>
        <DialogHeader><DialogTitle>Edit setting</DialogTitle></DialogHeader>
        <form className="space-y-4" onSubmit={async (event) => { event.preventDefault(); await onSubmit(value); setValue('') }}>
          <label className="space-y-1.5 text-sm font-medium text-text-primary">Key<Input value={setting?.key ?? ''} readOnly /></label>
          <label className="space-y-1.5 text-sm font-medium text-text-primary">Value<Textarea rows={6} value={value} onChange={(event) => setValue(event.target.value)} /></label>
          <DialogFooter><Button type="button" variant="outline" onClick={onClose}>Cancel</Button><Button type="submit" isLoading={pending}>Save</Button></DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
