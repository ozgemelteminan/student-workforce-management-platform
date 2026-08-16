import { zodResolver } from '@hookform/resolvers/zod'
import { ClipboardList, CopyPlus, Pencil, Plus, RefreshCw, Trash2 } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, Badge, Button, Card, CardContent, Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, EmptyState, ErrorState, Input, PageHeader, SearchInput, Skeleton, Textarea } from '../../components/ui'
import { useTaskLookups } from '../../features/tasks/useTaskQueries'
import { difficultyLabels, formatDuration, priorityLabels } from '../../features/tasks/taskPresentation'
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
                      <Badge variant="brand">{priorityLabels[template.defaultPriority]}</Badge>
                      <Badge variant="info">{difficultyLabels[template.defaultDifficulty]}</Badge>
                      <Badge>{formatDuration(template.estimatedDurationMinutes)}</Badge>
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

      <TemplateDialog template={editing} categories={lookups.categories.data ?? []} skills={lookups.skills.data ?? []} pending={mutations.create.isPending || mutations.update.isPending} onClose={() => setEditing(null)} onSubmit={async (payload) => {
        if (!editing) return
        if (editing.id) await mutations.update.mutateAsync({ id: editing.id, payload })
        else await mutations.create.mutateAsync(payload)
        setEditing(null)
      }} />
      <CreateTaskDialog template={taskSource} semesters={lookups.semesters.data ?? []} pending={mutations.createTask.isPending} onClose={() => setTaskSource(null)} onSubmit={async (values) => {
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

function TemplateDialog({ template, categories, skills, pending, onClose, onSubmit }: { template: TaskTemplate | null; categories: { id: string; name: string }[]; skills: { id: string; name: string }[]; pending: boolean; onClose: () => void; onSubmit: (payload: TemplatePayload) => Promise<void> }) {
  const form = useForm<TemplateFormValues>({ resolver: zodResolver(templateSchema), values: toFormValues(template, categories[0]?.id ?? '') })
  const [checklistItems, setChecklistItems] = useState<string[]>([])
  const [newChecklistItem, setNewChecklistItem] = useState('')
  const [selectedSkillIds, setSelectedSkillIds] = useState<string[]>([])
  const [skillSearch, setSkillSearch] = useState('')
  useEffect(() => {
    setChecklistItems(parseChecklistTemplate(template?.checklistTemplateJson))
    setSelectedSkillIds(parseRequiredSkillsTemplate(template?.requiredSkillsTemplateJson))
    setNewChecklistItem('')
    setSkillSearch('')
  }, [template])
  const filteredSkills = useMemo(() => skills.filter((skill) => skill.name.toLowerCase().includes(skillSearch.toLowerCase().trim())), [skillSearch, skills])
  const addChecklistItem = () => {
    const title = newChecklistItem.trim()
    if (!title) return
    setChecklistItems((items) => [...items, title])
    setNewChecklistItem('')
  }
  const submit = form.handleSubmit((values) => onSubmit(cleanTemplate(values, checklistItems, selectedSkillIds)))
  return (
    <Dialog open={Boolean(template)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{template?.id ? 'Edit template' : 'New template'}</DialogTitle>
          <DialogDescription>Define reusable defaults for future department tasks.</DialogDescription>
        </DialogHeader>
        <form className="space-y-4" onSubmit={submit}>
          <Label title="Title" error={form.formState.errors.title?.message}><Input {...form.register('title')} invalid={Boolean(form.formState.errors.title)} /></Label>
          <Label title="Description"><Textarea rows={4} {...form.register('description')} /></Label>
          <div className="grid gap-3 md:grid-cols-[minmax(14rem,1.4fr)_minmax(8rem,0.8fr)_minmax(8rem,0.8fr)]">
            <Label title="Category" error={form.formState.errors.categoryId?.message}><select className="h-9 w-full rounded-md border border-border bg-surface px-3 text-sm" {...form.register('categoryId')}>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></Label>
            <Label title="Priority"><select className="h-9 w-full rounded-md border border-border bg-surface px-3 text-sm" {...form.register('defaultPriority')}>{priorities.map((value) => <option key={value} value={value}>{priorityLabels[value]}</option>)}</select></Label>
            <Label title="Difficulty"><select className="h-9 w-full rounded-md border border-border bg-surface px-3 text-sm" {...form.register('defaultDifficulty')}>{difficulties.map((value) => <option key={value} value={value}>{difficultyLabels[value]}</option>)}</select></Label>
          </div>
          <Label title="Estimated duration" error={form.formState.errors.estimatedDurationMinutes?.message}><Input type="number" min={1} aria-label="Estimated duration in minutes" {...form.register('estimatedDurationMinutes')} /><span className="block text-xs text-text-secondary">{formatDuration(Number(form.watch('estimatedDurationMinutes') || 0))}</span></Label>
          <div className="space-y-2">
            <p className="text-sm font-medium text-text-primary">Checklist</p>
            <div className="flex gap-2"><Input value={newChecklistItem} onChange={(event) => setNewChecklistItem(event.target.value)} placeholder="Checklist item" /><Button type="button" variant="outline" onClick={addChecklistItem}>Add</Button></div>
            <div className="space-y-1">{checklistItems.map((item, index) => <div key={`${item}-${index}`} className="flex items-center justify-between gap-2 rounded-md border border-border px-3 py-2 text-sm"><span>{item}</span><Button type="button" size="sm" variant="ghost" onClick={() => setChecklistItems((items) => items.filter((_, itemIndex) => itemIndex !== index))}>Remove</Button></div>)}</div>
          </div>
          <div className="space-y-2">
            <p className="text-sm font-medium text-text-primary">Required skills</p>
            <SearchInput label="Search skills" value={skillSearch} onChange={(event) => setSkillSearch(event.target.value)} onClear={() => setSkillSearch('')} placeholder="Search skills" />
            <div className="max-h-44 space-y-1 overflow-auto rounded-md border border-border p-2">{filteredSkills.length ? filteredSkills.map((skill) => <label key={skill.id} className="flex items-center gap-2 rounded px-2 py-1 text-sm hover:bg-surface-secondary"><input type="checkbox" checked={selectedSkillIds.includes(skill.id)} onChange={(event) => setSelectedSkillIds((ids) => event.target.checked ? [...ids, skill.id] : ids.filter((id) => id !== skill.id))} />{skill.name}</label>) : <p className="px-2 py-1 text-sm text-text-secondary">No skills found.</p>}</div>
          </div>
          <DialogFooter><Button variant="outline" onClick={onClose}>Cancel</Button><Button type="submit" isLoading={pending}>Save</Button></DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function CreateTaskDialog({ template, semesters, pending, onClose, onSubmit }: { template: TaskTemplate | null; semesters: { id: string; name: string }[]; pending: boolean; onClose: () => void; onSubmit: (values: CreateTaskValues) => Promise<void> }) {
  const form = useForm<CreateTaskValues>({ resolver: zodResolver(createTaskSchema), values: { startDate: '', deadline: '', semesterId: '' } })
  const submit = form.handleSubmit((values) => onSubmit({ ...values, startDate: values.startDate ? new Date(values.startDate).toISOString() : undefined, deadline: new Date(values.deadline).toISOString(), semesterId: values.semesterId || undefined }))
  return (
    <Dialog open={Boolean(template)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader><DialogTitle>Create task from template</DialogTitle><DialogDescription>Review the timing and semester before creating the task.</DialogDescription></DialogHeader>
        <form className="space-y-4" onSubmit={submit}>
          <Label title="Template"><Input value={template?.title ?? ''} readOnly /></Label>
          <Label title="Start date (optional)"><Input type="datetime-local" {...form.register('startDate')} /></Label>
          <Label title="Deadline" error={form.formState.errors.deadline?.message}><Input type="datetime-local" {...form.register('deadline')} /></Label>
          <Label title="Semester (optional)"><select className="h-9 w-full rounded-md border border-border bg-surface px-3 text-sm" {...form.register('semesterId')}><option value="">No semester</option>{semesters.map((semester) => <option key={semester.id} value={semester.id}>{semester.name}</option>)}</select></Label>
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
  return { title: template?.title ?? '', description: template?.description ?? '', categoryId: template?.categoryId || fallbackCategoryId, defaultPriority: template?.defaultPriority ?? 'MEDIUM', defaultDifficulty: template?.defaultDifficulty ?? 'MEDIUM', estimatedDurationMinutes: template?.estimatedDurationMinutes ?? 60 }
}

function cleanTemplate(values: TemplateFormValues, checklistItems: string[], selectedSkillIds: string[]): TemplatePayload {
  return {
    ...values,
    description: values.description || undefined,
    checklistTemplateJson: checklistItems.length ? JSON.stringify(checklistItems.map((title, order) => ({ title, order }))) : undefined,
    requiredSkillsTemplateJson: selectedSkillIds.length ? JSON.stringify(selectedSkillIds.map((skillId) => ({ skillId, minimumLevel: 'BEGINNER' }))) : undefined,
  }
}

function parseChecklistTemplate(value?: string) {
  if (!value) return []
  try {
    const parsed = JSON.parse(value) as unknown
    if (!Array.isArray(parsed)) return []
    return parsed.map((item) => typeof item === 'string' ? item : typeof item === 'object' && item && 'title' in item ? String((item as { title?: unknown }).title ?? '') : '').filter(Boolean)
  } catch {
    return []
  }
}

function parseRequiredSkillsTemplate(value?: string) {
  if (!value) return []
  try {
    const parsed = JSON.parse(value) as unknown
    if (!Array.isArray(parsed)) return []
    return parsed.map((item) => typeof item === 'string' ? item : typeof item === 'object' && item && 'skillId' in item ? String((item as { skillId?: unknown }).skillId ?? '') : '').filter(Boolean)
  } catch {
    return []
  }
}

function TemplateSkeleton() {
  return <div className="space-y-3">{Array.from({ length: 3 }).map((_, index) => <Skeleton key={index} className="h-28" />)}</div>
}
