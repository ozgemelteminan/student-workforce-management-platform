import { Check, RotateCcw, Send, X } from 'lucide-react'
import { useMemo, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Badge, Button, Card, CardContent, CardHeader, DataTable, ErrorState, FormField, Input, MissingData, PageHeader, Pagination, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, Textarea } from '../../components/ui'
import type { RequestFilters, RequestStatus, RequestType, TaskRequest } from '../../features/requests/types'
import { useRequestMutations, useRequests } from '../../features/requests/useRequestQueries'
import { useStudents } from '../../features/students/useStudentQueries'
import { TaskDeadline } from '../../features/tasks/components'
import type { Task, TaskStatus } from '../../features/tasks/types'
import { useMyTasks } from '../../features/tasks/useTaskQueries'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatIstanbulDateTime } from '../../lib/date-time'

const requestTypes: RequestType[] = ['EXTENSION', 'REASSIGNMENT']
const requestStatuses: RequestStatus[] = ['PENDING', 'APPROVED', 'REJECTED', 'CANCELLED']
const requestEligibleTaskStatuses: TaskStatus[] = ['ASSIGNED', 'ACCEPTED', 'IN_PROGRESS', 'SUBMITTED_FOR_REVIEW', 'OVERDUE']

export function RequestsPage() {
  const { user } = useAuth()
  const roles = user?.roles ?? []
  const isStudent = roles.includes('STUDENT')
  const canReview = roles.includes('ADMIN') || roles.includes('TASK_MANAGER')
  const [searchParams, setSearchParams] = useSearchParams()
  const filters = useMemo(() => filtersFromSearch(searchParams), [searchParams])
  const query = useRequests(filters)
  const mutations = useRequestMutations()
  const myTasks = useMyTasks({ page: 1, pageSize: 50, sortBy: 'deadline', sortDirection: 'asc' }, isStudent)
  const students = useStudents({ page: 1, pageSize: 100, sortBy: 'name', sortDirection: 'asc' }, canReview)

  const updateFilters = (patch: Partial<RequestFilters>) => setSearchParams(filtersToSearch({ ...filters, ...patch }))

  const columns = [
    { key: 'type', header: 'Type', cell: (item: TaskRequest) => <Badge variant={item.type === 'EXTENSION' ? 'info' : 'brand'}>{formatRequestType(item.type)}</Badge> },
    { key: 'task', header: 'Task', cell: (item: TaskRequest) => <div className="min-w-44"><p className="font-medium">{item.taskTitle ?? 'Task'}</p>{item.requestedByName ? <p className="text-xs text-text-muted">{item.requestedByName}</p> : null}</div> },
    { key: 'status', header: 'Status', cell: (item: TaskRequest) => <RequestStatusBadge status={item.status} /> },
    { key: 'deadline', header: 'Deadline', cell: (item: TaskRequest) => item.requestedDeadline ? formatIstanbulDateTime(item.requestedDeadline) : <MissingData kind="not-set" /> },
    { key: 'created', header: 'Created', cell: (item: TaskRequest) => formatIstanbulDateTime(item.createdAt), className: 'hidden lg:table-cell' },
    { key: 'reason', header: 'Reason', cell: (item: TaskRequest) => <p className="max-w-sm truncate">{item.reason}</p>, className: 'hidden xl:table-cell' },
  ]

  return (
    <div className="space-y-5">
      <PageHeader title="Requests" description="Submit and review extension or reassignment requests without applying reassignment until approval." />
      <div className="grid gap-4 xl:grid-cols-[0.8fr_1.2fr]">
        <Card>
          <CardHeader><h2 className="text-sm font-semibold">{isStudent ? 'Create request' : 'Review context'}</h2></CardHeader>
          <CardContent>{isStudent ? <RequestCreateForm tasks={myTasks.data?.items ?? []} /> : <p className="text-sm text-text-secondary">Staff can approve or reject pending requests from the queue. Reassignment approval may require a new assignee.</p>}</CardContent>
        </Card>
        <Card>
          <CardContent className="grid gap-3 md:grid-cols-4">
            <FormField label="Type">{({ id }) => (
              <Select value={filters.type ?? 'any'} onValueChange={(value) => updateFilters({ type: value === 'any' ? undefined : value as RequestType, page: 1 })}>
                <SelectTrigger id={id}><SelectValue /></SelectTrigger>
                <SelectContent><SelectItem value="any">Any type</SelectItem>{requestTypes.map((type) => <SelectItem key={type} value={type}>{type}</SelectItem>)}</SelectContent>
              </Select>
            )}</FormField>
            <FormField label="Status">{({ id }) => (
              <Select value={filters.status ?? 'any'} onValueChange={(value) => updateFilters({ status: value === 'any' ? undefined : value as RequestStatus, page: 1 })}>
                <SelectTrigger id={id}><SelectValue /></SelectTrigger>
                <SelectContent><SelectItem value="any">Any status</SelectItem>{requestStatuses.map((status) => <SelectItem key={status} value={status}>{status}</SelectItem>)}</SelectContent>
              </Select>
            )}</FormField>
            <FormField label="Task ID">{({ id }) => <Input id={id} value={filters.taskId ?? ''} onChange={(event) => updateFilters({ taskId: event.target.value || undefined, page: 1 })} />}</FormField>
            <div className="self-end"><Button variant="ghost" onClick={() => setSearchParams(new URLSearchParams())}>Clear filters</Button></div>
          </CardContent>
        </Card>
      </div>
      {query.isError ? <ErrorState title="Could not load requests." description="The request queue is unavailable." retryAction={<Button variant="outline" onClick={() => void query.refetch()}>Retry</Button>} /> : (
        <DataTable
          columns={columns}
          rows={query.data?.items ?? []}
          getRowKey={(item) => item.id}
          isLoading={query.isLoading}
          emptyState={<p className="rounded-lg border border-border bg-surface p-8 text-center text-sm text-text-secondary">No requests match the current filters.</p>}
          rowActions={(item) => <RequestActions item={item} canReview={canReview} canCancel={isStudent} students={students.data?.items ?? []} mutations={mutations} />}
          pagination={query.data ? <Pagination {...query.data} onPageChange={(page) => updateFilters({ page })} /> : undefined}
        />
      )}
    </div>
  )
}

function RequestCreateForm({ tasks }: { tasks: Task[] }) {
  const mutations = useRequestMutations()
  const eligibleTasks = tasks.filter((task) => requestEligibleTaskStatuses.includes(task.status))
  const [type, setType] = useState<RequestType>('EXTENSION')
  const [taskId, setTaskId] = useState('')
  const [requestedDeadline, setRequestedDeadline] = useState('')
  const [reason, setReason] = useState('')
  const [suggestedStudentId, setSuggestedStudentId] = useState('')
  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!taskId || !reason.trim()) return
    if (type === 'EXTENSION') {
      if (!requestedDeadline) return
      mutations.createExtension.mutate({ taskId, requestedDeadline: new Date(requestedDeadline).toISOString(), reason })
    } else {
      mutations.createReassignment.mutate({ taskId, reason, suggestedStudentId: suggestedStudentId || undefined })
    }
  }
  const selectedTask = eligibleTasks.find((task) => task.id === taskId)
  return (
    <form className="space-y-3" onSubmit={submit}>
      <FormField label="Request type">{({ id }) => <Select value={type} onValueChange={(value) => setType(value as RequestType)}><SelectTrigger id={id}><SelectValue /></SelectTrigger><SelectContent>{requestTypes.map((item) => <SelectItem key={item} value={item}>{item}</SelectItem>)}</SelectContent></Select>}</FormField>
      <FormField label="Task">{({ id }) => <Select value={taskId} disabled={eligibleTasks.length === 0} onValueChange={setTaskId}><SelectTrigger id={id}><SelectValue placeholder="Select assigned task" /></SelectTrigger><SelectContent>{eligibleTasks.map((task) => <SelectItem key={task.id} value={task.id}>{task.title}</SelectItem>)}</SelectContent></Select>}</FormField>
      {eligibleTasks.length === 0 ? <p className="text-sm text-text-secondary">No active assigned tasks can receive new requests.</p> : null}
      {selectedTask ? <p className="text-xs text-text-secondary">Current deadline: <TaskDeadline task={selectedTask} compact /></p> : null}
      {type === 'EXTENSION' ? <FormField label="Requested deadline" required>{({ id }) => <Input id={id} type="datetime-local" value={requestedDeadline} onChange={(event) => setRequestedDeadline(event.target.value)} />}</FormField> : null}
      {type === 'REASSIGNMENT' ? <FormField label="Suggested student ID" helperText="Optional. The current API accepts only an ID here.">{({ id }) => <Input id={id} value={suggestedStudentId} onChange={(event) => setSuggestedStudentId(event.target.value)} />}</FormField> : null}
      <FormField label="Reason" required>{({ id }) => <Textarea id={id} value={reason} onChange={(event) => setReason(event.target.value)} />}</FormField>
      <Button type="submit" iconBefore={<Send aria-hidden="true" className="h-4 w-4" />} isLoading={mutations.createExtension.isPending || mutations.createReassignment.isPending} disabled={!taskId || !reason.trim() || (type === 'EXTENSION' && !requestedDeadline)}>Submit request</Button>
    </form>
  )
}

function RequestActions({ item, canReview, canCancel, students, mutations }: { item: TaskRequest; canReview: boolean; canCancel: boolean; students: { id: string; firstName: string; lastName: string }[]; mutations: ReturnType<typeof useRequestMutations> }) {
  const [comment, setComment] = useState('')
  const [newAssigneeId, setNewAssigneeId] = useState('')
  if (item.status !== 'PENDING') return null
  return (
    <div className="flex flex-wrap gap-2">
      {canReview ? (
        <>
          {item.type === 'REASSIGNMENT' ? <Select value={newAssigneeId} onValueChange={setNewAssigneeId}><SelectTrigger className="w-44" aria-label="New assignee"><SelectValue placeholder="New assignee" /></SelectTrigger><SelectContent>{students.map((student) => <SelectItem key={student.id} value={student.id}>{student.firstName} {student.lastName}</SelectItem>)}</SelectContent></Select> : null}
          <Input className="h-8 w-44" placeholder="Reviewer comment" value={comment} onChange={(event) => setComment(event.target.value)} />
          <Button size="sm" iconBefore={<Check aria-hidden="true" className="h-4 w-4" />} disabled={item.type === 'REASSIGNMENT' && !newAssigneeId} onClick={() => mutations.approve.mutate({ id: item.id, reviewerComment: comment || undefined, newAssigneeId: newAssigneeId || undefined })}>Approve</Button>
          <Button size="sm" variant="outline" iconBefore={<X aria-hidden="true" className="h-4 w-4" />} disabled={!comment.trim()} onClick={() => mutations.reject.mutate({ id: item.id, reviewerComment: comment })}>Reject</Button>
        </>
      ) : null}
      {canCancel ? <Button size="sm" variant="ghost" iconBefore={<RotateCcw aria-hidden="true" className="h-4 w-4" />} onClick={() => mutations.cancel.mutate(item.id)}>Cancel</Button> : null}
    </div>
  )
}

function RequestStatusBadge({ status }: { status: RequestStatus }) {
  const variant = status === 'APPROVED' ? 'success' : status === 'REJECTED' ? 'danger' : status === 'PENDING' ? 'warning' : 'neutral'
  return <Badge variant={variant}>{formatRequestStatus(status)}</Badge>
}

function formatRequestType(type: RequestType) {
  return type === 'EXTENSION' ? 'Extension' : 'Reassignment'
}

function formatRequestStatus(status: RequestStatus) {
  return status.charAt(0) + status.slice(1).toLowerCase()
}

function filtersFromSearch(searchParams: URLSearchParams): RequestFilters {
  return {
    page: Number(searchParams.get('page') ?? 1),
    pageSize: Number(searchParams.get('pageSize') ?? 20),
    taskId: searchParams.get('taskId') ?? undefined,
    type: (searchParams.get('type') ?? undefined) as RequestType | undefined,
    status: (searchParams.get('status') ?? undefined) as RequestStatus | undefined,
  }
}

function filtersToSearch(filters: RequestFilters) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  return search
}
