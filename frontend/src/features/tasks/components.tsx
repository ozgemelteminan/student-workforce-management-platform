import { AlertTriangle, CalendarClock, CheckCircle2, Clock3, FileText, MoreHorizontal, UserRound } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Badge, Button, Card, CardContent, CardHeader, DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger, EmptyState, IconButton } from '../../components/ui'
import { formatIstanbulDateTime } from '../../lib/date-time'
import { cn } from '../../lib/utils/cn'
import { deadlineLabel, formatDuration, priorityLabels, priorityVariant, statusLabels, statusVariant } from './taskPresentation'
import type { Task, TaskChecklistItem } from './types'

export function TaskStatusBadge({ status }: { status: Task['status'] }) {
  return <Badge variant={statusVariant(status)}>{statusLabels[status]}</Badge>
}

export function TaskPriorityBadge({ priority }: { priority: Task['priority'] }) {
  return <Badge variant={priorityVariant(priority)}>{priorityLabels[priority]}</Badge>
}

export function TaskDeadline({ task, compact = false }: { task: Pick<Task, 'deadline' | 'status'>; compact?: boolean }) {
  const deadline = deadlineLabel(task)
  return (
    <span className={cn('inline-flex items-center gap-1.5 text-sm', deadline.tone === 'danger' && 'text-destructive', deadline.tone === 'warning' && 'text-warning', deadline.tone === 'neutral' && 'text-text-secondary')}>
      <CalendarClock aria-hidden="true" className="h-4 w-4" />
      <span title={deadline.absolute}>{compact ? deadline.label : `${deadline.label} · ${deadline.absolute}`}</span>
    </span>
  )
}

export function ChecklistProgress({ items }: { items: TaskChecklistItem[] | undefined }) {
  const total = items?.length ?? 0
  const completed = items?.filter((item) => item.isCompleted).length ?? 0
  return (
    <div className="space-y-1.5">
      <div className="flex items-center justify-between text-xs text-text-secondary">
        <span>Checklist</span>
        <span>{completed} of {total}</span>
      </div>
      <div className="h-2 rounded-full bg-surface-secondary">
        <div className="h-2 rounded-full bg-brand" style={{ width: total ? `${Math.round((completed / total) * 100)}%` : '0%' }} />
      </div>
    </div>
  )
}

export function TaskSummary({ task, checklist }: { task: Task; checklist?: TaskChecklistItem[] }) {
  return (
    <Card>
      <CardHeader className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <h2 className="truncate text-base font-semibold text-text-primary">{task.title}</h2>
          <p className="mt-1 text-sm text-text-secondary">{task.description || 'No description provided.'}</p>
        </div>
        <div className="flex shrink-0 flex-wrap gap-2">
          <TaskStatusBadge status={task.status} />
          <TaskPriorityBadge priority={task.priority} />
        </div>
      </CardHeader>
      <CardContent className="grid gap-4 md:grid-cols-3">
        <Info icon={<Clock3 className="h-4 w-4" />} label="Estimate" value={formatDuration(task.estimatedDurationMinutes)} />
        <Info icon={<UserRound className="h-4 w-4" />} label="Assignee" value={formatTaskAssignee(task)} />
        <Info icon={<FileText className="h-4 w-4" />} label="Updated" value={formatIstanbulDateTime(task.updatedAt)} />
        <div className="md:col-span-3">
          <TaskDeadline task={task} />
        </div>
        <div className="md:col-span-3">
          <ChecklistProgress items={checklist} />
        </div>
      </CardContent>
    </Card>
  )
}

export function formatTaskAssignee(task: Pick<Task, 'assignedStudentId' | 'assignedStudentName'>) {
  return task.assignedStudentName || (task.assignedStudentId ? 'Name unavailable' : 'Not set')
}

export function TaskActionsMenu({ task, canEdit, onCancel }: { task: Task; canEdit: boolean; onCancel?: () => void }) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <IconButton label={`Actions for ${task.title}`} icon={<MoreHorizontal aria-hidden="true" className="h-4 w-4" />} />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem asChild>
          <Link to={`/tasks/${task.id}`}>Open detail</Link>
        </DropdownMenuItem>
        {canEdit ? (
          <DropdownMenuItem asChild>
            <Link to={`/tasks/${task.id}/edit`}>Edit</Link>
          </DropdownMenuItem>
        ) : null}
        {canEdit && task.status !== 'CANCELLED' && task.status !== 'COMPLETED' ? (
          <DropdownMenuItem destructive onSelect={onCancel}>Cancel task</DropdownMenuItem>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

export function TaskEmpty({ canCreate }: { canCreate: boolean }) {
  return (
    <EmptyState
      icon={<CheckCircle2 aria-hidden="true" className="h-5 w-5" />}
      title="No tasks found."
      description="Adjust filters or check another predefined view."
      primaryAction={canCreate ? <Link className="inline-flex h-9 items-center justify-center rounded-md border border-brand bg-brand px-4 text-sm font-medium text-text-inverse hover:bg-brand-hover focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand" to="/tasks/new">Create task</Link> : undefined}
    />
  )
}

export function ConflictMessage({ onReload }: { onReload: () => void }) {
  return (
    <div role="alert" className="rounded-md border border-warning/30 bg-warning/10 px-3 py-2 text-sm text-text-primary">
      <div className="flex items-start gap-2">
        <AlertTriangle aria-hidden="true" className="mt-0.5 h-4 w-4 text-warning" />
        <div>
          <p className="font-medium">This task changed on the server.</p>
          <p className="mt-1 text-text-secondary">Reload the latest version before saving or taking another workflow action.</p>
          <Button className="mt-2" size="sm" variant="outline" onClick={onReload}>Reload latest</Button>
        </div>
      </div>
    </div>
  )
}

function Info({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <div className="flex items-center gap-2 text-sm">
      <span className="text-text-muted">{icon}</span>
      <span className="text-text-muted">{label}</span>
      <span className="min-w-0 truncate font-medium text-text-primary">{value}</span>
    </div>
  )
}
