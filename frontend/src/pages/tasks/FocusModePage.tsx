import { ArrowRight, Target } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Button, Card, CardContent, CardHeader, EmptyState, ErrorState, PageHeader } from '../../components/ui'
import { focusModeTasks } from '../../features/tasks/attention'
import { TaskDeadline, TaskPriorityBadge, TaskStatusBadge } from '../../features/tasks/components'
import { useMyTasks } from '../../features/tasks/useTaskQueries'

export function FocusModePage() {
  const tasks = useMyTasks({ page: 1, pageSize: 20, sortBy: 'deadline', sortDirection: 'asc' })
  const focused = focusModeTasks(tasks.data?.items ?? []).slice(0, 6)
  return (
    <div className="mx-auto max-w-5xl space-y-5">
      <PageHeader title="Focus Mode" description="Your immediate work, reduced to the next practical tasks. No timer, no time tracking." secondaryActions={<Button variant="outline" onClick={() => window.location.assign('/tasks?view=my')}>All my tasks</Button>} />
      <Card>
        <CardHeader className="flex flex-row items-center gap-2"><Target aria-hidden="true" className="h-4 w-4 text-brand" /><h2 className="text-sm font-semibold">My day</h2></CardHeader>
        <CardContent className="space-y-3">
          {tasks.isError ? <ErrorState title="Could not load your tasks." description="Try again from the task workspace." retryAction={<Button variant="outline" onClick={() => void tasks.refetch()}>Retry</Button>} /> : null}
          {!tasks.isLoading && !tasks.isError && focused.length === 0 ? <EmptyState title="No focused work." description="Your accessible task list has no active items right now." /> : null}
          {focused.map((task) => (
            <Link key={task.id} to={`/tasks/${task.id}`} className="flex items-center justify-between gap-3 rounded-md border border-border px-4 py-3 hover:bg-surface-secondary focus-visible:outline focus-visible:outline-2 focus-visible:outline-brand">
              <div className="min-w-0">
                <p className="truncate font-medium">{task.title}</p>
                <div className="mt-2 flex flex-wrap items-center gap-2"><TaskStatusBadge status={task.status} /><TaskPriorityBadge priority={task.priority} /><TaskDeadline task={task} compact /></div>
              </div>
              <ArrowRight aria-hidden="true" className="h-4 w-4 text-text-muted" />
            </Link>
          ))}
        </CardContent>
      </Card>
    </div>
  )
}
