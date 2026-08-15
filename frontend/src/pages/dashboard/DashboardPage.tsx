import { AlertTriangle, ArrowRight, ClipboardList, Store, Target } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'
import { Badge, Button, Card, CardContent, CardHeader, EmptyState, ErrorState, PageHeader, Skeleton } from '../../components/ui'
import { deriveAttentionItems, focusModeTasks } from '../../features/tasks/attention'
import { TaskDeadline, TaskPriorityBadge, TaskStatusBadge } from '../../features/tasks/components'
import { useTasks, useMyTasks } from '../../features/tasks/useTaskQueries'
import { useMarketplaceListings } from '../../features/marketplace/useMarketplaceQueries'
import { useAuth } from '../../lib/auth/AuthProvider'

export function DashboardPage() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const roles = user?.roles ?? []
  const isStudent = roles.includes('STUDENT')
  const isStaff = roles.includes('ADMIN') || roles.includes('TASK_MANAGER')
  const canReview = roles.includes('ADMIN') || roles.includes('REVIEWER')
  const baseFilters = { page: 1, pageSize: 12, sortBy: 'deadline' as const, sortDirection: 'asc' as const }
  const myTasks = useMyTasks(baseFilters, isStudent)
  const allTasks = useTasks(baseFilters, !isStudent)
  const tasks = isStudent ? myTasks : allTasks
  const reviewTasks = useTasks({ ...baseFilters, status: 'SUBMITTED_FOR_REVIEW' }, canReview)
  const marketplace = useMarketplaceListings({ page: 1, pageSize: 6, status: 'PUBLISHED' })
  const taskItems = tasks.data?.items ?? []
  const attention = deriveAttentionItems([...(tasks.data?.items ?? []), ...(reviewTasks.data?.items ?? [])], roles).slice(0, 8)
  const focusTasks = focusModeTasks(taskItems).slice(0, 4)

  return (
    <div className="space-y-5">
      <PageHeader title="Dashboard" description="Operational priorities from real task and task pool data." primaryAction={isStaff ? <Button iconBefore={<ClipboardList aria-hidden="true" className="h-4 w-4" />} onClick={() => navigate('/tasks/new')}>Create task</Button> : <Button variant="outline" iconBefore={<Target aria-hidden="true" className="h-4 w-4" />} onClick={() => navigate('/focus')}>Focus mode</Button>} />
      <div className="grid gap-5 xl:grid-cols-[minmax(0,1.4fr)_minmax(20rem,0.8fr)]">
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold">Needs attention</h2>
          </CardHeader>
          <CardContent>
            {tasks.isLoading || reviewTasks.isLoading ? <div className="space-y-2"><Skeleton className="h-14" /><Skeleton className="h-14" /><Skeleton className="h-14" /></div> : null}
            {tasks.isError ? <ErrorState title="Could not load task attention." description="Retry from the task workspace." navigationAction={<Button variant="outline" onClick={() => void tasks.refetch()}>Retry</Button>} /> : null}
            {!tasks.isLoading && !tasks.isError && attention.length === 0 ? <EmptyState title="No urgent task items." description="Nothing in the current accessible task data needs immediate attention." /> : null}
            <div className="space-y-2">
              {attention.map((item) => (
                <Link key={item.id} to={item.href} className="flex items-start justify-between gap-3 rounded-md border border-border px-3 py-3 hover:bg-surface-secondary focus-visible:outline focus-visible:outline-2 focus-visible:outline-brand">
                  <div className="min-w-0">
                    <div className="flex items-center gap-2">
                      <AlertTriangle aria-hidden="true" className={item.tone === 'danger' ? 'h-4 w-4 text-destructive' : 'h-4 w-4 text-warning'} />
                      <p className="truncate text-sm font-medium text-text-primary">{item.title}</p>
                    </div>
                    <p className="mt-1 text-sm text-text-secondary">{item.description}</p>
                  </div>
                  <ArrowRight aria-hidden="true" className="mt-1 h-4 w-4 text-text-muted" />
                </Link>
              ))}
            </div>
          </CardContent>
        </Card>
        <div className="space-y-5">
          <Metric title="Active accessible tasks" value={String(tasks.data?.totalCount ?? 0)} loading={tasks.isLoading} />
          <Metric title="Open task pool listings" value={String(marketplace.data?.totalCount ?? 0)} loading={marketplace.isLoading} />
          {canReview ? <Metric title="Submissions waiting review" value={String(reviewTasks.data?.totalCount ?? 0)} loading={reviewTasks.isLoading} /> : null}
        </div>
      </div>
      <div className="grid gap-5 xl:grid-cols-2">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <h2 className="text-sm font-semibold">Upcoming work</h2>
            <Link className="text-sm font-medium text-brand hover:text-brand-hover" to={isStudent ? '/focus' : '/tasks'}>{isStudent ? 'Open focus mode' : 'Open tasks'}</Link>
          </CardHeader>
          <CardContent className="space-y-3">
            {focusTasks.length === 0 && !tasks.isLoading ? <EmptyState title="No upcoming tasks." description="The current task query returned no active work." /> : null}
            {focusTasks.map((task) => (
              <Link key={task.id} to={`/tasks/${task.id}`} className="block rounded-md border border-border px-3 py-3 hover:bg-surface-secondary">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <p className="font-medium text-text-primary">{task.title}</p>
                  <TaskStatusBadge status={task.status} />
                </div>
                <div className="mt-2 flex flex-wrap items-center gap-3">
                  <TaskPriorityBadge priority={task.priority} />
                  <TaskDeadline task={task} compact />
                </div>
              </Link>
            ))}
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <h2 className="text-sm font-semibold">Task pool activity</h2>
            <Link className="text-sm font-medium text-brand hover:text-brand-hover" to="/marketplace">Open task pool</Link>
          </CardHeader>
          <CardContent className="space-y-3">
            {marketplace.isError ? <ErrorState title="Could not load task pool." description="Task pool data can be retried from its workspace." retryAction={<Button variant="outline" onClick={() => void marketplace.refetch()}>Retry</Button>} /> : null}
            {!marketplace.isLoading && !marketplace.isError && marketplace.data?.items.length === 0 ? <EmptyState icon={<Store className="h-5 w-5" />} title="No open task pool listings." /> : null}
            {marketplace.data?.items.slice(0, 5).map((listing) => (
              <Link key={listing.id} to={`/tasks/${listing.taskId}`} className="flex items-center justify-between rounded-md border border-border px-3 py-2 hover:bg-surface-secondary">
                <span className="truncate text-sm font-medium">Task {listing.taskId.slice(0, 8)}</span>
                <Badge variant={listing.status === 'PUBLISHED' ? 'success' : 'neutral'}>{listing.status}</Badge>
              </Link>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function Metric({ title, value, loading }: { title: string; value: string; loading: boolean }) {
  return (
    <Card>
      <CardContent>
        <p className="text-sm text-text-secondary">{title}</p>
        <p className="mt-2 text-2xl font-semibold text-text-primary">{loading ? '—' : value}</p>
      </CardContent>
    </Card>
  )
}
