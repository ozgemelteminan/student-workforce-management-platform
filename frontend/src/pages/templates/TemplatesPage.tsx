import { zodResolver } from '@hookform/resolvers/zod'
import { ClipboardList, CopyPlus, Pencil, Plus, RefreshCw, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, Badge, Button, Card, CardContent, Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, EmptyState, ErrorState, Input, PageHeader, SearchInput, Skeleton, Textarea } from '../../components/ui'
import { useTaskLookups } from '../../features/tasks/useTaskQueries'
import { useTemplateMutations, useTemplates } from '../../features/templates/useTemplateQueries'
import type { TaskTemplate, TemplatePayload } from '../../features/templates/types'
import { formatIstanbulDateTime } from '../../lib/date-time'

const priorities = ['LOW', 'MEDIUM', 'HIGH', 'URGENT'] as const
const difficulties = ['EASY', 'MEDIUM', 'HARD'] as const

const templateSchema = z.object({
  title: z.string().trim().min(1).max(200),
  description: z.string().max(8000).optional(),
  categoryId: z.string().min(1),
  defaultPriority: z.enum(priorities),
  defaultDifficulty: z.enum(difficulties),
  estimatedDurationMinutes: z.coerce.number().int().positive(),
  checklistTemplateJson: z.string().optional(),
  requiredSkillsTemplateJson: z.string().optional(),
})

const createTaskSchema = z.object({
  startDate: z.string().optional(),
  deadline: z.string().trim().min(1),
  semesterId: z.string().optional(),
})

type TemplateFormValues = z.infer<typeof templateSchema>
type CreateTaskValues = z.infer<typeof createTaskSchema>

export function TemplatesPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [editing, setEditing] = useState<TaskTemplate | null>(null)
  const [deleting, setDeleting] = useState<TaskTemplate | null>(null)
  const [taskSource, setTaskSource] = useState<TaskTemplate | null>(null)
  const templates = useTemplates({ page: 1, pageSize: 40, search })
  const lookups = useTaskLookups()
  const mutations = useTemplateMutations()

  return (
    <div className="space-y-5">
      <PageHeader title="Templates" description="Reusable task blueprints managed through the task-template contract." primaryAction={<Button iconBefore={<Plus aria-hidden="true" className="h-4 w-4" />} onClick={() => setEditing(emptyTemplate())}>New template</Button>} />
      <Card>
        <CardContent className="space-y-4">
          <SearchInput label="Search templates" placeholder="Search templates" value={search} onChange={(event) => setSearch(event.target.value)} onClear={() => setSearch('')} className="md:w-80" />
          {templates.isError ? <ErrorState title="Templates could not be loaded." description="Refresh the workspace or check your access." retryAction={<Button variant="outline" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={() => void templates.refetch()}>Retry</Button>} /> : null}
          {templates.isLoading ? <TemplateSkeleton /> : null}
          {!templates.isLoading && !templates.isError && !templates.data?.items.length ? <EmptyState title="No templates yet." description="Create a template to speed up recurring department work." className="min-h-48" /> : null}
          <div className="grid gap-3">
            {templates.data?.items.map((template) => (
              <article key={template.id} className="rounded-lg border border-border bg-surface px-4 py-4">
                <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                  <div className="min-w-0 space-y-2">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge variant="brand">{template.defaultPriority}</Badge>
                      <Badge variant="info">{template.defaultDifficulty}</Badge>
                      <Badge>{template.estimatedDurationMinutes} min</Badge>
                    </div>
                    <h2 className="text-base font-semibold text-text-primary">{template.title}</h2>
                    {template.description ? <p className="line-clamp-2 whitespace-pre-wrap text-sm text-text-secondary">{template.description}</p> : null}
                    <p className="text-xs text-text-muted">Created {formatIstanbulDateTime(template.createdAt)}</p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button variant="outline" size="sm" iconBefore={<CopyPlus aria-hidden="true" className="h-4 w-4" />} onClick={() => setTaskSource(template)}>Create task</Button>
                    <Button variant="ghost" size="sm" iconBefore={<Pencil aria-hidden="true" className="h-4 w-4" />} onClick={() => setEditing(template)}>Edit</Button>
                    <Button variant="ghost" size="sm" iconBefore={<Trash2 aria-hidden="true" className="h-4 w-4" />} onClick={() => setDeleting(template)}>Delete</Button>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </CardContent>
      </Card>

      <TemplateDialog template={editing} categories={lookups.categories.data ?? []} pending={mutations.create.isPending || mutations.update.isPending} onClose={() => setEditing(null)} onSubmit={async (payload) => {
        if (!editing) return
        if (editing.id) await mutations.update.mutateAsync({ id: editing.id, payload })
        else await mutations.create.mutateAsync(payload)
        setEditing(null)
      }} />
      <CreateTaskDialog template={taskSource} pending={mutations.createTask.isPending} onClose={() => setTaskSource(null)} onSubmit={async (values) => {
        if (!taskSource) return
        const task = await mutations.createTask.mutateAsync({ id: taskSource.id, payload: { ...values, startDate: values.startDate || undefined, semesterId: values.semesterId || undefined } })
        setTaskSource(null)
        void navigate(`/tasks/${task.id}`)
      }} />
      <AlertDialog open={Boolean(deleting)} onOpenChange={(open) => !open && setDeleting(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete template?</AlertDialogTitle>
            <AlertDialogDescription>Templates used by recurring tasks will be rejected by the server.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={() => deleting && mutations.delete.mutate(deleting.id, { onSettled: () => setDeleting(null) })}>Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function TemplateDialog({ template, categories, pending, onClose, onSubmit }: { template: TaskTemplate | null; categories: { id: string; name: string }[]; pending: boolean; onClose: () => void; onSubmit: (payload: TemplatePayload) => Promise<void> }) {
  const form = useForm<TemplateFormValues>({ resolver: zodResolver(templateSchema), values: toFormValues(template, categories[0]?.id ?? '') })
  return (
    <Dialog open={Boolean(template)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{template?.id ? 'Edit template' : 'New template'}</DialogTitle>
          <DialogDescription>Only backend-supported template fields are editable here.</DialogDescription>
        </DialogHeader>
        <form className="space-y-4" onSubmit={form.handleSubmit((values) => onSubmit(cleanTemplate(values)))}>
          <Label title="Title" error={form.formState.errors.title?.message}><Input {...form.register('title')} invalid={Boolean(form.formState.errors.title)} /></Label>
          <Label title="Description"><Textarea rows={4} {...form.register('description')} /></Label>
          <div className="grid gap-3 md:grid-cols-3">
            <Label title="Category" error={form.formState.errors.categoryId?.message}><select className="h-9 w-full rounded-md border border-border bg-surface px-3 text-sm" {...form.register('categoryId')}>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></Label>
            <Label title="Priority"><select className="h-9 w-full rounded-md border border-border bg-surface px-3 text-sm" {...form.register('defaultPriority')}>{priorities.map((value) => <option key={value} value={value}>{value}</option>)}</select></Label>
            <Label title="Difficulty"><select className="h-9 w-full rounded-md border border-border bg-surface px-3 text-sm" {...form.register('defaultDifficulty')}>{difficulties.map((value) => <option key={value} value={value}>{value}</option>)}</select></Label>
          </div>
          <Label title="Estimated duration minutes" error={form.formState.errors.estimatedDurationMinutes?.message}><Input type="number" min={1} {...form.register('estimatedDurationMinutes')} /></Label>
          <Label title="Checklist template JSON"><Textarea rows={3} {...form.register('checklistTemplateJson')} /></Label>
          <Label title="Required skills template JSON"><Textarea rows={3} {...form.register('requiredSkillsTemplateJson')} /></Label>
          <DialogFooter><Button variant="outline" onClick={onClose}>Cancel</Button><Button type="submit" isLoading={pending}>Save</Button></DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function CreateTaskDialog({ template, pending, onClose, onSubmit }: { template: TaskTemplate | null; pending: boolean; onClose: () => void; onSubmit: (values: CreateTaskValues) => Promise<void> }) {
  const form = useForm<CreateTaskValues>({ resolver: zodResolver(createTaskSchema), values: { startDate: '', deadline: '', semesterId: '' } })
  return (
    <Dialog open={Boolean(template)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader><DialogTitle>Create task from template</DialogTitle><DialogDescription>The server copies supported defaults and returns the created task.</DialogDescription></DialogHeader>
        <form className="space-y-4" onSubmit={form.handleSubmit(onSubmit)}>
          <Label title="Template"><Input value={template?.title ?? ''} readOnly /></Label>
          <Label title="Start date UTC (optional)"><Input placeholder="2026-08-14T09:00:00Z" {...form.register('startDate')} /></Label>
          <Label title="Deadline UTC" error={form.formState.errors.deadline?.message}><Input placeholder="2026-08-21T17:00:00Z" {...form.register('deadline')} /></Label>
          <Label title="Semester ID (optional)"><Input {...form.register('semesterId')} /></Label>
          <DialogFooter><Button variant="outline" onClick={onClose}>Cancel</Button><Button type="submit" iconBefore={<ClipboardList aria-hidden="true" className="h-4 w-4" />} isLoading={pending}>Create task</Button></DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function Label({ title, error, children }: { title: string; error?: string; children: React.ReactNode }) {
  return <label className="space-y-1.5 text-sm font-medium text-text-primary">{title}{children}{error ? <span className="block text-xs text-destructive">{error}</span> : null}</label>
}

function emptyTemplate(): TaskTemplate {
  return { id: '', title: '', categoryId: '', defaultPriority: 'MEDIUM', defaultDifficulty: 'MEDIUM', estimatedDurationMinutes: 60, createdById: '', createdAt: new Date().toISOString() }
}

function toFormValues(template: TaskTemplate | null, fallbackCategoryId: string): TemplateFormValues {
  return { title: template?.title ?? '', description: template?.description ?? '', categoryId: template?.categoryId || fallbackCategoryId, defaultPriority: template?.defaultPriority ?? 'MEDIUM', defaultDifficulty: template?.defaultDifficulty ?? 'MEDIUM', estimatedDurationMinutes: template?.estimatedDurationMinutes ?? 60, checklistTemplateJson: template?.checklistTemplateJson ?? '', requiredSkillsTemplateJson: template?.requiredSkillsTemplateJson ?? '' }
}

function cleanTemplate(values: TemplateFormValues): TemplatePayload {
  return { ...values, description: values.description || undefined, checklistTemplateJson: values.checklistTemplateJson || undefined, requiredSkillsTemplateJson: values.requiredSkillsTemplateJson || undefined }
}

function TemplateSkeleton() {
  return <div className="space-y-3">{Array.from({ length: 3 }).map((_, index) => <Skeleton key={index} className="h-28" />)}</div>
}
