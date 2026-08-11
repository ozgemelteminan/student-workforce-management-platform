import { zodResolver } from '@hookform/resolvers/zod'
import { fromZonedTime, toZonedTime } from 'date-fns-tz'
import { ArrowLeft, Save } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate, useParams } from 'react-router-dom'
import { z } from 'zod'
import { Button, Card, CardContent, ErrorState, FormField, Input, PageHeader, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, Textarea } from '../../components/ui'
import { ConflictMessage } from '../../features/tasks/components'
import { DISPLAY_TIME_ZONE } from '../../lib/date-time'
import { ApiError } from '../../lib/api'
import { useAuth } from '../../lib/auth/AuthProvider'
import { useTask, useTaskLookups, useTaskMutations } from '../../features/tasks/useTaskQueries'
import type { TaskDifficulty, TaskPriority } from '../../features/tasks/types'

const schema = z.object({
  title: z.string().trim().min(1, 'Title is required.').max(200, 'Title must be 200 characters or fewer.'),
  description: z.string().max(8000, 'Description must be 8000 characters or fewer.').optional(),
  categoryId: z.string().uuid('Choose a category.'),
  semesterId: z.string().optional(),
  priority: z.enum(['LOW', 'MEDIUM', 'HIGH', 'URGENT']),
  difficulty: z.enum(['EASY', 'MEDIUM', 'HARD']),
  startDate: z.string().optional(),
  deadline: z.string().min(1, 'Deadline is required.'),
  estimatedDurationMinutes: z.coerce.number().int().positive('Estimate must be greater than 0.'),
}).refine((value) => !value.startDate || new Date(toUtc(value.deadline)).getTime() > new Date(toUtc(value.startDate)).getTime(), { path: ['deadline'], message: 'Deadline must be after start date.' })

type FormValues = z.infer<typeof schema>

export function TaskFormPage() {
  const { taskId } = useParams()
  const isEdit = Boolean(taskId)
  const { user } = useAuth()
  const navigate = useNavigate()
  const canManage = user?.roles.includes('ADMIN') || user?.roles.includes('TASK_MANAGER')
  const task = useTask(taskId)
  const lookups = useTaskLookups()
  const mutations = useTaskMutations(taskId)
  const [conflict, setConflict] = useState(false)
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { title: '', description: '', categoryId: '', semesterId: '', priority: 'MEDIUM', difficulty: 'EASY', startDate: '', deadline: '', estimatedDurationMinutes: 60 },
  })

  useEffect(() => {
    if (task.data) {
      form.reset({
        title: task.data.title,
        description: task.data.description ?? '',
        categoryId: task.data.categoryId,
        semesterId: task.data.semesterId ?? '',
        priority: task.data.priority,
        difficulty: task.data.difficulty,
        startDate: task.data.startDate ? toLocalInput(task.data.startDate) : '',
        deadline: toLocalInput(task.data.deadline),
        estimatedDurationMinutes: task.data.estimatedDurationMinutes,
      })
    }
  }, [form, task.data])

  useEffect(() => {
    const listener = (event: BeforeUnloadEvent) => {
      if (form.formState.isDirty) event.preventDefault()
    }
    window.addEventListener('beforeunload', listener)
    return () => window.removeEventListener('beforeunload', listener)
  }, [form.formState.isDirty])

  const categories = lookups.categories.data ?? []
  const submit = form.handleSubmit(async (values) => {
    setConflict(false)
    const payload = {
      title: values.title,
      description: values.description || undefined,
      categoryId: values.categoryId,
      semesterId: values.semesterId || undefined,
      priority: values.priority as TaskPriority,
      difficulty: values.difficulty as TaskDifficulty,
      startDate: values.startDate ? toUtc(values.startDate) : undefined,
      deadline: toUtc(values.deadline),
      estimatedDurationMinutes: values.estimatedDurationMinutes,
    }
    try {
      const saved = isEdit && task.data ? await mutations.update.mutateAsync({ id: task.data.id, payload: { ...payload, concurrencyToken: task.data.concurrencyToken } }) : await mutations.create.mutateAsync(payload)
      form.reset(values)
      navigate(`/tasks/${saved.id}`)
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setConflict(true)
        void task.refetch()
      }
    }
  })

  const loading = isEdit && task.isLoading
  const title = isEdit ? 'Edit task' : 'Create task'
  const metadata = useMemo(() => isEdit && task.data ? `${task.data.id.slice(0, 8)} · concurrency protected` : undefined, [isEdit, task.data])

  if (!canManage) return <ErrorState title="Permission denied." description="Task creation and editing are available to staff task management roles." navigationAction={<Button variant="outline" onClick={() => navigate('/tasks')}>Back to tasks</Button>} />
  if (task.isError) return <ErrorState title="Task not available." description="The task could not be loaded for editing." retryAction={<Button variant="outline" onClick={() => void task.refetch()}>Retry</Button>} />

  return (
    <div className="space-y-5">
      <PageHeader title={title} description="Use backend-supported fields only. Deadlines are entered in Europe/Istanbul and sent as UTC instants." metadata={metadata} secondaryActions={<Button variant="outline" iconBefore={<ArrowLeft className="h-4 w-4" />} onClick={() => navigate(isEdit && taskId ? `/tasks/${taskId}` : '/tasks')}>Back</Button>} />
      <Card>
        <CardContent>
          <form className="grid gap-4 lg:grid-cols-2" onSubmit={submit} noValidate>
            {conflict ? <div className="lg:col-span-2"><ConflictMessage onReload={() => void task.refetch()} /></div> : null}
            <FormField label="Title" error={form.formState.errors.title?.message} required>{({ id, describedBy, invalid }) => <Input id={id} disabled={loading} invalid={invalid} aria-describedby={describedBy} {...form.register('title')} />}</FormField>
            <FormField label="Category" error={form.formState.errors.categoryId?.message} required>{({ id, describedBy }) => (
              <Select value={form.watch('categoryId')} onValueChange={(value) => form.setValue('categoryId', value, { shouldDirty: true, shouldValidate: true })}>
                <SelectTrigger id={id} aria-describedby={describedBy}><SelectValue placeholder={lookups.categories.isLoading ? 'Loading categories' : 'Choose category'} /></SelectTrigger>
                <SelectContent>{categories.map((category) => <SelectItem key={category.id} value={category.id}>{category.name}</SelectItem>)}</SelectContent>
              </Select>
            )}</FormField>
            <FormField label="Priority" required>{({ id }) => <Select value={form.watch('priority')} onValueChange={(value) => form.setValue('priority', value as TaskPriority, { shouldDirty: true })}><SelectTrigger id={id}><SelectValue /></SelectTrigger><SelectContent>{['LOW', 'MEDIUM', 'HIGH', 'URGENT'].map((value) => <SelectItem key={value} value={value}>{value}</SelectItem>)}</SelectContent></Select>}</FormField>
            <FormField label="Difficulty" required>{({ id }) => <Select value={form.watch('difficulty')} onValueChange={(value) => form.setValue('difficulty', value as TaskDifficulty, { shouldDirty: true })}><SelectTrigger id={id}><SelectValue /></SelectTrigger><SelectContent>{['EASY', 'MEDIUM', 'HARD'].map((value) => <SelectItem key={value} value={value}>{value}</SelectItem>)}</SelectContent></Select>}</FormField>
            <FormField label="Start date" error={form.formState.errors.startDate?.message}>{({ id, describedBy, invalid }) => <Input id={id} type="datetime-local" invalid={invalid} aria-describedby={describedBy} {...form.register('startDate')} />}</FormField>
            <FormField label="Deadline" error={form.formState.errors.deadline?.message} required>{({ id, describedBy, invalid }) => <Input id={id} type="datetime-local" invalid={invalid} aria-describedby={describedBy} {...form.register('deadline')} />}</FormField>
            <FormField label="Estimated duration" error={form.formState.errors.estimatedDurationMinutes?.message} helperText="Minutes. Example: 90 for 1 hr 30 min." required>{({ id, describedBy, invalid }) => <Input id={id} type="number" min={1} invalid={invalid} aria-describedby={describedBy} {...form.register('estimatedDurationMinutes')} />}</FormField>
            <FormField label="Description" className="lg:col-span-2" error={form.formState.errors.description?.message}>{({ id, describedBy, invalid }) => <Textarea id={id} invalid={invalid} aria-describedby={describedBy} {...form.register('description')} />}</FormField>
            <div className="flex justify-end gap-2 lg:col-span-2">
              <Button variant="outline" onClick={() => navigate(isEdit && taskId ? `/tasks/${taskId}` : '/tasks')}>Discard</Button>
              <Button type="submit" isLoading={mutations.create.isPending || mutations.update.isPending} iconBefore={<Save className="h-4 w-4" />}>Save task</Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}

function toUtc(value: string) {
  return fromZonedTime(value, DISPLAY_TIME_ZONE).toISOString()
}

function toLocalInput(value: string) {
  const local = toZonedTime(new Date(value), DISPLAY_TIME_ZONE)
  return `${local.getFullYear()}-${String(local.getMonth() + 1).padStart(2, '0')}-${String(local.getDate()).padStart(2, '0')}T${String(local.getHours()).padStart(2, '0')}:${String(local.getMinutes()).padStart(2, '0')}`
}
