import { Plus } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Badge, Button, Card, CardContent, DataTable, ErrorState, Pagination, PageHeader, SearchInput, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, Sheet, SheetBody, SheetContent, SheetHeader, SheetTitle } from '../../components/ui'
import { formatTaskAssignee, TaskActionsMenu, TaskDeadline, TaskEmpty, TaskPriorityBadge, TaskStatusBadge, TaskSummary } from '../../features/tasks/components'
import { filtersFromSearchParams, filtersToSearchParams, type TaskView } from '../../features/tasks/taskFilters'
import { formatDuration } from '../../features/tasks/taskPresentation'
import type { Task, TaskFilters } from '../../features/tasks/types'
import { useTask, useTaskCollections, useTasks, useMyTasks } from '../../features/tasks/useTaskQueries'
import { useAuth } from '../../lib/auth/AuthProvider'

const viewLabels: Record<TaskView, string> = {
  all: 'All tasks',
  my: 'My tasks',
  overdue: 'Overdue',
  'due-week': 'Due this week',
  'needs-review': 'Needs review',
  unassigned: 'Unassigned',
}

export function TasksPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const roles = user?.roles ?? []
  const canCreate = roles.includes('ADMIN') || roles.includes('TASK_MANAGER')
  const filters = useMemo(() => filtersFromSearchParams(searchParams), [searchParams])
  const [draftSearch, setDraftSearch] = useState(filters.search ?? '')
  const [previewId, setPreviewId] = useState<string | null>(searchParams.get('preview'))
  const myTasks = useMyTasks(filters, filters.view === 'my')
  const allTasks = useTasks(filters, filters.view !== 'my')
  const query = filters.view === 'my' ? myTasks : allTasks
  const preview = useTask(previewId ?? undefined)
  const previewCollections = useTaskCollections(previewId ?? undefined, canCreate)

  useEffect(() => {
    const handle = window.setTimeout(() => updateFilters({ search: draftSearch || undefined, page: 1 }), 350)
    return () => window.clearTimeout(handle)
  }, [draftSearch])

  const updateFilters = (patch: Partial<TaskFilters & { view: TaskView }>) => {
    const next = { ...filters, ...patch }
    setSearchParams(filtersToSearchParams(next))
  }

  const columns = [
    { key: 'title', header: 'Task', cell: (task: Task) => <div className="min-w-56"><p className="font-medium">{task.title}</p><p className="truncate text-xs text-text-muted">{task.description || 'No description'}</p></div> },
    { key: 'status', header: 'Status', cell: (task: Task) => <TaskStatusBadge status={task.status} /> },
    { key: 'priority', header: 'Priority', cell: (task: Task) => <TaskPriorityBadge priority={task.priority} /> },
    { key: 'deadline', header: 'Deadline', cell: (task: Task) => <TaskDeadline task={task} compact /> },
    { key: 'estimate', header: 'Estimate', cell: (task: Task) => formatDuration(task.estimatedDurationMinutes), className: 'hidden lg:table-cell' },
    { key: 'assignee', header: 'Assignee', cell: (task: Task) => formatTaskAssignee(task), className: 'hidden xl:table-cell' },
  ]

  return (
    <div className="space-y-5">
      <PageHeader title="Tasks" description="Search, prioritize, preview, and manage department task work." primaryAction={canCreate ? <Button iconBefore={<Plus aria-hidden="true" className="h-4 w-4" />} onClick={() => navigate('/tasks/new')}>Create task</Button> : undefined} />
      <Card>
        <CardContent className="space-y-4">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-end">
            <div className="min-w-64 flex-1">
              <SearchInput label="Search tasks" value={draftSearch} onChange={(event) => setDraftSearch(event.target.value)} onClear={() => setDraftSearch('')} placeholder="Search title or description" />
            </div>
            <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
              <Select value={filters.view} onValueChange={(value) => updateFilters({ view: value as TaskView, page: 1 })}>
                <SelectTrigger aria-label="Task view"><SelectValue /></SelectTrigger>
                <SelectContent>{Object.entries(viewLabels).map(([value, label]) => <SelectItem key={value} value={value}>{label}</SelectItem>)}</SelectContent>
              </Select>
              <Select value={filters.status ?? 'any'} onValueChange={(value) => updateFilters({ status: value === 'any' ? undefined : value as Task['status'], page: 1 })}>
                <SelectTrigger aria-label="Status filter"><SelectValue placeholder="Status" /></SelectTrigger>
                <SelectContent>{['any', 'ASSIGNED', 'ACCEPTED', 'IN_PROGRESS', 'SUBMITTED_FOR_REVIEW', 'COMPLETED', 'INCOMPLETE', 'CANNOT_COMPLETE', 'CANCELLED', 'OVERDUE'].map((status) => <SelectItem key={status} value={status}>{status === 'any' ? 'Any status' : status}</SelectItem>)}</SelectContent>
              </Select>
              <Select value={filters.priority ?? 'any'} onValueChange={(value) => updateFilters({ priority: value === 'any' ? undefined : value as Task['priority'], page: 1 })}>
                <SelectTrigger aria-label="Priority filter"><SelectValue placeholder="Priority" /></SelectTrigger>
                <SelectContent>{['any', 'LOW', 'MEDIUM', 'HIGH', 'URGENT'].map((priority) => <SelectItem key={priority} value={priority}>{priority === 'any' ? 'Any priority' : priority}</SelectItem>)}</SelectContent>
              </Select>
              <Select value={filters.sortBy ?? 'deadline'} onValueChange={(value) => updateFilters({ sortBy: value as TaskFilters['sortBy'], page: 1 })}>
                <SelectTrigger aria-label="Sort field"><SelectValue /></SelectTrigger>
                <SelectContent>{['deadline', 'priority', 'created', 'workload'].map((sort) => <SelectItem key={sort} value={sort}>{sort}</SelectItem>)}</SelectContent>
              </Select>
              <Select value={filters.sortDirection ?? 'asc'} onValueChange={(value) => updateFilters({ sortDirection: value as 'asc' | 'desc', page: 1 })}>
                <SelectTrigger aria-label="Sort direction"><SelectValue /></SelectTrigger>
                <SelectContent><SelectItem value="asc">Ascending</SelectItem><SelectItem value="desc">Descending</SelectItem></SelectContent>
              </Select>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="neutral">{query.data?.totalCount ?? 0} results</Badge>
            {filters.view !== 'all' ? <Badge variant="brand">{viewLabels[filters.view]}</Badge> : null}
            {(filters.search || filters.status || filters.priority) ? <Button variant="ghost" size="sm" onClick={() => { setDraftSearch(''); setSearchParams(new URLSearchParams()) }}>Clear filters</Button> : null}
          </div>
        </CardContent>
      </Card>
      {query.isError ? <ErrorState title="Could not load tasks." description="Check your connection and try again." retryAction={<Button variant="outline" onClick={() => void query.refetch()}>Retry</Button>} /> : (
        <DataTable
          columns={columns}
          rows={query.data?.items ?? []}
          getRowKey={(task) => task.id}
          isLoading={query.isLoading}
          emptyState={<TaskEmpty canCreate={canCreate} />}
          onRowClick={(task) => setPreviewId(task.id)}
          rowActions={(task) => <TaskActionsMenu task={task} canEdit={canCreate} />}
          pagination={query.data ? <Pagination {...query.data} onPageChange={(page) => updateFilters({ page })} /> : undefined}
        />
      )}
      <Sheet open={Boolean(previewId)} onOpenChange={(open) => { if (!open) setPreviewId(null) }}>
        <SheetContent>
          <SheetHeader>
            <SheetTitle>Task preview</SheetTitle>
          </SheetHeader>
          <SheetBody className="space-y-4">
            {preview.isError ? <ErrorState title="Could not load preview." description="Open the task detail or retry." retryAction={<Button variant="outline" onClick={() => void preview.refetch()}>Retry</Button>} /> : null}
            {preview.data ? <TaskSummary task={preview.data} checklist={previewCollections.checklist.data} /> : null}
            {preview.data ? <Button className="w-full" onClick={() => navigate(`/tasks/${preview.data.id}`)}>Open full details</Button> : null}
          </SheetBody>
        </SheetContent>
      </Sheet>
    </div>
  )
}
