import { CalendarOff, Clock, RefreshCw, Send } from 'lucide-react'
import { useState } from 'react'
import { Badge, Button, Card, CardContent, EmptyState, ErrorState, Input, PageHeader, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, Skeleton, Textarea } from '../../components/ui'
import { timesheetStatusLabels } from '../../features/collaboration/collaborationPresentation'
import { useCurrentTimesheet, useCollaborationMutations, useTimesheets, useUnavailability } from '../../features/collaboration/useCollaborationQueries'
import { useCurrentStudent } from '../../features/students/useStudentQueries'
import { useMyTasks } from '../../features/tasks/useTaskQueries'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatDateOnly, formatIstanbulDateTime } from '../../lib/date-time'
import type { TimesheetStatus } from '../../features/collaboration/types'
import type { ReactNode } from 'react'

const statuses: TimesheetStatus[] = ['DRAFT', 'SUBMITTED', 'APPROVED', 'NEEDS_CORRECTION']

export function WorkloadPage() {
  const { user } = useAuth()
  const roles = user?.roles ?? []
  const staff = roles.includes('ADMIN') || roles.includes('TASK_MANAGER')
  const studentRole = roles.includes('STUDENT')
  const currentStudent = useCurrentStudent(studentRole)
  const currentWeek = useCurrentTimesheet()
  const myTasks = useMyTasks({ page: 1, pageSize: 50, sortBy: 'deadline', sortDirection: 'asc' }, studentRole)
  const [status, setStatus] = useState<TimesheetStatus | 'ALL'>('SUBMITTED')
  const timesheets = useTimesheets({ page: 1, pageSize: 30, status: status === 'ALL' ? undefined : status })
  const unavailability = useUnavailability(currentStudent.data?.student.id ? { studentId: currentStudent.data.student.id } : {})
  const mutations = useCollaborationMutations()
  const [entry, setEntry] = useState({ taskId: '', workDate: new Date().toISOString().slice(0, 10), minutes: 30, note: '' })
  const [away, setAway] = useState({ startAt: '', endAt: '', category: 'Unavailable', note: '' })

  return (
    <div className="space-y-5">
      <PageHeader title="Workload" description="Track weekly work, submit timesheets, review submitted weeks, and record temporary unavailability." metadata={<Badge variant="info">Europe/Istanbul</Badge>} />
      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="space-y-5">
          {studentRole ? (
            <Card>
              <CardContent className="space-y-4">
                <SectionHeading icon={<Clock className="h-4 w-4" />} title="Current week" />
                {currentWeek.isLoading ? <Skeleton className="h-40" /> : null}
                {currentWeek.isError ? <ErrorState title="Current timesheet could not be loaded." description="Refresh the workload view or check your access." retryAction={<Button variant="outline" iconBefore={<RefreshCw className="h-4 w-4" />} onClick={() => void currentWeek.refetch()}>Retry</Button>} /> : null}
                {currentWeek.data ? (
                  <div className="space-y-4">
                    <div className="grid gap-3 md:grid-cols-4">
                      <Metric label="Week" value={`${formatDateOnly(currentWeek.data.weekStartDate)} - ${formatDateOnly(currentWeek.data.weekEndDate)}`} />
                      <Metric label="Status" value={timesheetStatusLabels[currentWeek.data.status]} />
                      <Metric label="Logged" value={`${currentWeek.data.totalMinutes} min`} />
                      <Metric label="Target" value={currentWeek.data.targetMinutes > 0 ? `${currentWeek.data.targetMinutes} min` : 'Not configured'} />
                    </div>
                    <div className="divide-y divide-border rounded-lg border border-border">
                      {currentWeek.data.entries.map((item) => (
                        <div key={item.id} className="grid gap-2 px-3 py-2 text-sm md:grid-cols-[1fr_auto] md:items-center">
                          <div><p className="font-medium">{formatDateOnly(item.workDate)} · {item.minutes} min</p><p className="text-text-secondary">{item.taskTitle ?? `Task ${item.taskId.slice(0, 8)}`}{item.note ? ` · ${item.note}` : ''}</p></div>
                          {currentWeek.data.status === 'DRAFT' || currentWeek.data.status === 'NEEDS_CORRECTION' ? <Button variant="ghost" size="sm" onClick={() => mutations.deleteEntry.mutate(item.id)}>Remove</Button> : null}
                        </div>
                      ))}
                      {currentWeek.data.entries.length === 0 ? <EmptyState title="No time entries yet." className="min-h-32" /> : null}
                    </div>
                    {currentWeek.data.status === 'DRAFT' || currentWeek.data.status === 'NEEDS_CORRECTION' ? (
                      <form className="grid gap-2 lg:grid-cols-[1.2fr_10rem_8rem_1fr_auto]" onSubmit={(event) => { event.preventDefault(); if (entry.taskId) void mutations.upsertEntry.mutateAsync({ ...entry, note: entry.note || undefined }) }}>
                        <Select value={entry.taskId} onValueChange={(taskId) => setEntry((value) => ({ ...value, taskId }))}><SelectTrigger aria-label="Timesheet task"><SelectValue placeholder="Task" /></SelectTrigger><SelectContent>{myTasks.data?.items.map((task) => <SelectItem key={task.id} value={task.id}>{task.title}</SelectItem>)}</SelectContent></Select>
                        <Input aria-label="Work date" type="date" value={entry.workDate} onChange={(event) => setEntry((value) => ({ ...value, workDate: event.target.value }))} />
                        <Input aria-label="Minutes" type="number" min={1} value={entry.minutes} onChange={(event) => setEntry((value) => ({ ...value, minutes: Number(event.target.value) }))} />
                        <Input aria-label="Entry note" value={entry.note} onChange={(event) => setEntry((value) => ({ ...value, note: event.target.value }))} placeholder="Note" />
                        <Button type="submit" isLoading={mutations.upsertEntry.isPending}>Add</Button>
                      </form>
                    ) : null}
                    <Button variant="outline" iconBefore={<Send className="h-4 w-4" />} disabled={currentWeek.data.entries.length === 0 || currentWeek.data.status === 'SUBMITTED' || currentWeek.data.status === 'APPROVED'} isLoading={mutations.submitWeek.isPending} onClick={() => mutations.submitWeek.mutate(currentWeek.data.id)}>Submit week</Button>
                  </div>
                ) : null}
              </CardContent>
            </Card>
          ) : null}
          {staff ? (
            <Card>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <SectionHeading icon={<Clock className="h-4 w-4" />} title="Timesheet review" />
                  <Select value={status} onValueChange={(value) => setStatus(value as TimesheetStatus | 'ALL')}><SelectTrigger className="w-48" aria-label="Timesheet status"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="ALL">All</SelectItem>{statuses.map((item) => <SelectItem key={item} value={item}>{timesheetStatusLabels[item]}</SelectItem>)}</SelectContent></Select>
                </div>
                {timesheets.isError ? <ErrorState title="Timesheets could not be loaded." description="Refresh the review queue or check your access." retryAction={<Button variant="outline" onClick={() => void timesheets.refetch()}>Retry</Button>} /> : null}
                {timesheets.isLoading ? <Skeleton className="h-52" /> : null}
                <div className="divide-y divide-border rounded-lg border border-border">
                  {timesheets.data?.items.map((week) => (
                    <div key={week.id} className="grid gap-3 px-3 py-3 text-sm lg:grid-cols-[1fr_auto] lg:items-center">
                      <div><p className="font-medium">{formatDateOnly(week.weekStartDate)} - {formatDateOnly(week.weekEndDate)} · {week.totalMinutes} min</p><p className="text-text-secondary">Student {week.studentId.slice(0, 8)} · {timesheetStatusLabels[week.status]}</p></div>
                      {week.status === 'SUBMITTED' ? <div className="flex gap-2"><Button size="sm" onClick={() => mutations.reviewWeek.mutate({ id: week.id, status: 'APPROVED' })}>Approve</Button><Button size="sm" variant="outline" onClick={() => mutations.reviewWeek.mutate({ id: week.id, status: 'NEEDS_CORRECTION', reviewerComment: 'Please revise this week.' })}>Needs correction</Button></div> : <Badge>{timesheetStatusLabels[week.status]}</Badge>}
                    </div>
                  ))}
                  {!timesheets.isLoading && !timesheets.data?.items.length ? <EmptyState title={status === 'SUBMITTED' ? 'No submitted timesheets.' : 'No timesheets found.'} className="min-h-32" /> : null}
                </div>
              </CardContent>
            </Card>
          ) : null}
        </div>
        <Card>
          <CardContent className="space-y-4">
            <SectionHeading icon={<CalendarOff className="h-4 w-4" />} title="Temporary unavailability" />
            <form className="space-y-2" onSubmit={(event) => { event.preventDefault(); if (away.startAt && away.endAt) void mutations.createUnavailability.mutateAsync({ startAt: toApiInstant(away.startAt), endAt: toApiInstant(away.endAt), category: away.category, note: away.note || undefined }) }}>
              <Label title="Start"><Input aria-label="Start" type="datetime-local" value={away.startAt} onChange={(event) => setAway((value) => ({ ...value, startAt: event.target.value }))} /></Label>
              <Label title="End"><Input aria-label="End" type="datetime-local" value={away.endAt} onChange={(event) => setAway((value) => ({ ...value, endAt: event.target.value }))} /></Label>
              <Label title="Unavailability type"><Input aria-label="Unavailability type" value={away.category} onChange={(event) => setAway((value) => ({ ...value, category: event.target.value }))} /></Label>
              <Label title="Reason"><Textarea aria-label="Reason" value={away.note} onChange={(event) => setAway((value) => ({ ...value, note: event.target.value }))} /></Label>
              <Button type="submit" className="w-full" isLoading={mutations.createUnavailability.isPending}>Save unavailability</Button>
            </form>
            <div className="space-y-2">
              {unavailability.data?.map((item) => <div key={item.id} className="rounded-md border border-border p-3 text-sm"><p className="font-medium">{item.category}</p><p className="text-text-secondary">{formatIstanbulDateTime(item.startAt)} - {formatIstanbulDateTime(item.endAt)}</p>{item.note ? <p className="mt-1 text-text-secondary">{item.note}</p> : null}<Button className="mt-2" size="sm" variant="ghost" onClick={() => mutations.deleteUnavailability.mutate(item.id)}>Remove</Button></div>)}
              {!unavailability.isLoading && !unavailability.data?.length ? <EmptyState title="No temporary unavailability." className="min-h-32" /> : null}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function SectionHeading({ icon, title }: { icon: ReactNode; title: string }) {
  return <h2 className="flex items-center gap-2 text-sm font-semibold text-text-primary">{icon}{title}</h2>
}

function Metric({ label, value }: { label: string; value: string }) {
  return <div className="rounded-md border border-border bg-surface-secondary p-3"><dt className="text-xs text-text-muted">{label}</dt><dd className="mt-1 text-sm font-semibold">{value}</dd></div>
}

function Label({ title, children }: { title: string; children: ReactNode }) {
  return <label className="space-y-1.5 text-sm font-medium text-text-primary">{title}{children}</label>
}

function toApiInstant(value: string) {
  return new Date(value).toISOString()
}
