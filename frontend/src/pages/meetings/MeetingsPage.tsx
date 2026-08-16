import { CalendarCheck, Plus, RefreshCw } from 'lucide-react'
import { useEffect, useState, type ReactNode } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Badge, Button, Card, CardContent, EmptyState, ErrorState, Input, PageHeader, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, Skeleton, Textarea } from '../../components/ui'
import { campusPresenceLabels, meetingStatusLabels, meetingTypeLabels } from '../../features/collaboration/collaborationPresentation'
import { useCollaborationMutations, useMeeting, useMeetings, useMeetingSlots } from '../../features/collaboration/useCollaborationQueries'
import { useTaskLookups } from '../../features/tasks/useTaskQueries'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatIstanbulDateTime } from '../../lib/date-time'
import type { CampusPresence, MeetingStatus, MeetingType } from '../../features/collaboration/types'

const meetingStatuses: MeetingStatus[] = ['DRAFT', 'AVAILABILITY_REQUESTED', 'CONFIRMED', 'CANCELLED']

export function MeetingsPage() {
  const { meetingId } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const roles = user?.roles ?? []
  const staff = roles.includes('ADMIN') || roles.includes('TASK_MANAGER')
  const [status, setStatus] = useState<MeetingStatus | 'ALL'>('ALL')
  const [selectedId, setSelectedId] = useState<string | undefined>(meetingId)
  const meetings = useMeetings({ page: 1, pageSize: 30, status: status === 'ALL' ? undefined : status })
  const selected = useMeeting(selectedId)
  const slots = useMeetingSlots(selectedId, Boolean(selectedId && staff))
  const lookups = useTaskLookups({ studentsEnabled: staff })
  const mutations = useCollaborationMutations()
  const [create, setCreate] = useState({ title: '', type: 'IN_PERSON' as MeetingType, responseDeadline: '', participantStudentIds: [] as string[], location: '', agenda: '' })
  const [response, setResponse] = useState({ campusPresence: 'UNSURE' as CampusPresence, startAt: '', endAt: '', note: '' })
  const [actionItem, setActionItem] = useState('')

  useEffect(() => {
    setSelectedId(meetingId)
  }, [meetingId])

  const selectMeeting = (id: string) => {
    setSelectedId(id)
    navigate(`/meetings/${id}`)
  }

  return (
    <div className="space-y-5">
      <PageHeader title="Meetings" description="Coordinate availability, recommend slots, confirm meetings, and track follow-up action items." metadata={<Badge variant="info">Europe/Istanbul</Badge>} />
      <div className="grid gap-5 xl:grid-cols-[minmax(0,0.9fr)_minmax(24rem,1.1fr)]">
        <Card>
          <CardContent className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h2 className="flex items-center gap-2 text-sm font-semibold"><CalendarCheck className="h-4 w-4" /> Meeting queue</h2>
              <Select value={status} onValueChange={(value) => setStatus(value as MeetingStatus | 'ALL')}><SelectTrigger className="w-48" aria-label="Meeting status"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="ALL">All</SelectItem>{meetingStatuses.map((item) => <SelectItem key={item} value={item}>{meetingStatusLabels[item]}</SelectItem>)}</SelectContent></Select>
            </div>
            {meetings.isError ? <ErrorState title="Meetings could not be loaded." description="Refresh the meeting queue or check your access." retryAction={<Button variant="outline" iconBefore={<RefreshCw className="h-4 w-4" />} onClick={() => void meetings.refetch()}>Retry</Button>} /> : null}
            {meetings.isLoading ? <Skeleton className="h-56" /> : null}
            <div className="divide-y divide-border rounded-lg border border-border">
              {meetings.data?.items.map((meeting) => (
                <button key={meeting.id} type="button" className="block w-full px-3 py-3 text-left text-sm transition hover:bg-surface-secondary focus-visible:outline focus-visible:outline-2 focus-visible:outline-brand" onClick={() => selectMeeting(meeting.id)}>
                  <span className="flex flex-wrap items-center gap-2"><span className="font-medium">{meeting.title}</span><Badge>{meetingStatusLabels[meeting.status]}</Badge><Badge variant="info">{meetingTypeLabels[meeting.type]}</Badge></span>
                  <span className="mt-1 block text-text-secondary">Respond by {formatIstanbulDateTime(meeting.responseDeadline)} · {meeting.participants.length} participants</span>
                </button>
              ))}
              {!meetings.isLoading && !meetings.data?.items.length ? <EmptyState title="No meetings found." className="min-h-40" /> : null}
            </div>
          </CardContent>
        </Card>
        <div className="space-y-5">
          {staff ? (
            <Card>
              <CardContent className="space-y-3">
                <h2 className="flex items-center gap-2 text-sm font-semibold"><Plus className="h-4 w-4" /> New meeting</h2>
                <form className="grid gap-2 lg:grid-cols-2" onSubmit={(event) => { event.preventDefault(); if (create.title && create.responseDeadline && create.participantStudentIds.length) void mutations.createMeeting.mutateAsync({ ...create, responseDeadline: toApiInstant(create.responseDeadline), location: create.location || undefined, agenda: create.agenda || undefined }) }}>
                  <Label title="Title"><Input aria-label="Title" value={create.title} onChange={(event) => setCreate((value) => ({ ...value, title: event.target.value }))} placeholder="Planning meeting" /></Label>
                  <Label title="Mode"><Select value={create.type} onValueChange={(type) => setCreate((value) => ({ ...value, type: type as MeetingType }))}><SelectTrigger aria-label="Mode"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="IN_PERSON">{meetingTypeLabels.IN_PERSON}</SelectItem><SelectItem value="ONLINE">{meetingTypeLabels.ONLINE}</SelectItem></SelectContent></Select></Label>
                  <Label title="Date & time"><Input aria-label="Date & time" type="datetime-local" value={create.responseDeadline} onChange={(event) => setCreate((value) => ({ ...value, responseDeadline: event.target.value }))} /></Label>
                  <Label title="Participants"><Select value={create.participantStudentIds[0] ?? ''} onValueChange={(studentId) => setCreate((value) => ({ ...value, participantStudentIds: [studentId] }))}><SelectTrigger aria-label="Participants"><SelectValue placeholder="Participant" /></SelectTrigger><SelectContent>{lookups.students.data?.items.map((student) => <SelectItem key={student.id} value={student.id}>{student.firstName} {student.lastName}</SelectItem>)}</SelectContent></Select></Label>
                  <Label title="Location / link"><Input aria-label="Location / link" value={create.location} onChange={(event) => setCreate((value) => ({ ...value, location: event.target.value }))} placeholder="Room or meeting link" /></Label>
                  <Label title="Agenda"><Textarea aria-label="Agenda" value={create.agenda} onChange={(event) => setCreate((value) => ({ ...value, agenda: event.target.value }))} placeholder="Agenda" /></Label>
                  <Button type="submit" className="lg:col-span-2" isLoading={mutations.createMeeting.isPending}>Create meeting</Button>
                </form>
              </CardContent>
            </Card>
          ) : null}
          <Card>
            <CardContent className="space-y-4">
              {!selectedId ? <EmptyState title="Select a meeting." description="Choose a meeting from the queue." className="min-h-32" /> : null}
              {selected.isLoading ? <Skeleton className="h-56" /> : null}
              {selected.data ? (
                <div className="space-y-4">
                  <div><div className="flex flex-wrap gap-2"><Badge>{meetingStatusLabels[selected.data.status]}</Badge><Badge variant="info">{meetingTypeLabels[selected.data.type]}</Badge></div><h2 className="mt-2 text-base font-semibold">{selected.data.title}</h2><p className="text-sm text-text-secondary">{selected.data.status === 'CONFIRMED' && selected.data.confirmedStartAt ? `Scheduled ${formatIstanbulDateTime(selected.data.confirmedStartAt)}` : `Respond by ${formatIstanbulDateTime(selected.data.responseDeadline)}`}</p></div>
                  <form className="grid gap-2 md:grid-cols-[12rem_1fr_1fr]" onSubmit={(event) => { event.preventDefault(); if (response.startAt && response.endAt) void mutations.respondMeeting.mutate({ id: selected.data.id, campusPresence: response.campusPresence, availableRangesJson: JSON.stringify([{ startAt: toApiInstant(response.startAt), endAt: toApiInstant(response.endAt) }]), note: response.note || undefined }) }}>
                    <Select value={response.campusPresence} onValueChange={(campusPresence) => setResponse((value) => ({ ...value, campusPresence: campusPresence as CampusPresence }))}><SelectTrigger aria-label="Campus presence"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="ON_CAMPUS">{campusPresenceLabels.ON_CAMPUS}</SelectItem><SelectItem value="OFF_CAMPUS">{campusPresenceLabels.OFF_CAMPUS}</SelectItem><SelectItem value="UNSURE">{campusPresenceLabels.UNSURE}</SelectItem></SelectContent></Select>
                    <Input aria-label="Available from" type="datetime-local" value={response.startAt} onChange={(event) => setResponse((value) => ({ ...value, startAt: event.target.value }))} />
                    <Input aria-label="Available until" type="datetime-local" value={response.endAt} onChange={(event) => setResponse((value) => ({ ...value, endAt: event.target.value }))} />
                    <Textarea aria-label="Availability note" className="md:col-span-3" value={response.note} onChange={(event) => setResponse((value) => ({ ...value, note: event.target.value }))} />
                    <Button type="submit" className="md:col-span-3" isLoading={mutations.respondMeeting.isPending}>Submit availability</Button>
                  </form>
                  {staff ? <div className="space-y-2"><h3 className="text-sm font-semibold">Recommended slots</h3>{slots.data?.map((slot) => <div key={`${slot.startAt}-${slot.endAt}`} className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-border p-3 text-sm"><span>{formatIstanbulDateTime(slot.startAt)} - {formatIstanbulDateTime(slot.endAt)} · {slot.availableCount}/{slot.participantCount} available</span><Button size="sm" variant="outline" onClick={() => mutations.confirmMeeting.mutate({ id: selected.data.id, startAt: slot.startAt, endAt: slot.endAt, location: selected.data.location })}>Confirm</Button></div>)}</div> : null}
                  <div className="space-y-2"><h3 className="text-sm font-semibold">Action items</h3>{selected.data.actionItems.map((item) => <div key={item.id} className="rounded-md border border-border p-3 text-sm"><p className="font-medium">{item.title}</p><p className="text-text-secondary">{item.taskId ? `Linked task ${item.taskId.slice(0, 8)}` : 'No task created'}</p></div>)}{staff ? <form className="flex gap-2" onSubmit={(event) => { event.preventDefault(); if (actionItem.trim()) void mutations.addActionItem.mutateAsync({ id: selected.data.id, title: actionItem }).then(() => setActionItem('')) }}><Input aria-label="Action item title" value={actionItem} onChange={(event) => setActionItem(event.target.value)} /><Button type="submit">Add</Button></form> : null}</div>
                </div>
              ) : null}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}

function toApiInstant(value: string) {
  return new Date(value).toISOString()
}

function Label({ title, children }: { title: string; children: ReactNode }) {
  return <label className="space-y-1.5 text-sm font-medium text-text-primary">{title}{children}</label>
}
