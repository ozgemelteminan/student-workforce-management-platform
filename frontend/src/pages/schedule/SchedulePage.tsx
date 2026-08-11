import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Badge, Button, Card, CardContent, CardHeader, DataTable, ErrorState, FormField, Input, MissingData, PageHeader, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, Textarea } from '../../components/ui'
import { useCurrentStudent, useStudents } from '../../features/students/useStudentQueries'
import { availabilityStatuses, dayOfWeekValues, formatTimeRange, isValidTimeRange } from '../../features/schedules/timeOnly'
import type { Availability, AvailabilityStatus, CourseSchedule, DayOfWeek } from '../../features/schedules/types'
import { useActiveSemester, useScheduleCollections, useScheduleMutations, useSemesters } from '../../features/schedules/useScheduleQueries'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatDateOnly } from '../../lib/date-time'

export function SchedulePage() {
  const { user } = useAuth()
  const roles = user?.roles ?? []
  const isStudent = roles.includes('STUDENT') && !roles.some((role) => role === 'ADMIN' || role === 'TASK_MANAGER')
  const students = useStudents({ page: 1, pageSize: 100, sortBy: 'name', sortDirection: 'asc' })
  const me = useCurrentStudent(isStudent)
  const semesters = useSemesters()
  const activeSemester = useActiveSemester()
  const [selectedStudentId, setSelectedStudentId] = useState('')
  const [semesterId, setSemesterId] = useState('')
  const [editingSchedule, setEditingSchedule] = useState<CourseSchedule | null>(null)
  const [editingAvailability, setEditingAvailability] = useState<Availability | null>(null)
  const studentId = isStudent ? me.data?.student.id : selectedStudentId
  const effectiveSemesterId = semesterId || activeSemester.data?.id
  const collections = useScheduleCollections(studentId, effectiveSemesterId)
  const mutations = useScheduleMutations(studentId)
  const selectedStudent = isStudent ? me.data?.student : students.data?.items.find((student) => student.id === selectedStudentId)

  const canManage = Boolean(studentId && effectiveSemesterId)

  const scheduleColumns = [
    { key: 'course', header: 'Course', cell: (item: CourseSchedule) => <div className="min-w-48"><p className="font-medium">{item.courseName}</p><p className="text-xs text-text-muted">{item.courseCode}</p></div> },
    { key: 'day', header: 'Day', cell: (item: CourseSchedule) => item.dayOfWeek },
    { key: 'time', header: 'Time', cell: (item: CourseSchedule) => formatTimeRange(item.startTime, item.endTime) },
    { key: 'location', header: 'Location', cell: (item: CourseSchedule) => item.location || <MissingData kind="not-set" /> },
  ]
  const availabilityColumns = [
    { key: 'day', header: 'Day', cell: (item: Availability) => item.dayOfWeek },
    { key: 'time', header: 'Time', cell: (item: Availability) => formatTimeRange(item.startTime, item.endTime) },
    { key: 'status', header: 'Status', cell: (item: Availability) => <Badge variant={item.status === 'AVAILABLE' ? 'success' : item.status === 'PREFERRED' ? 'info' : 'neutral'}>{item.status}</Badge> },
    { key: 'reason', header: 'Reason', cell: (item: Availability) => item.reason || <MissingData /> },
  ]

  return (
    <div className="space-y-5">
      <PageHeader title="Schedule" description="Coordinate semester course commitments and availability windows." metadata={selectedStudent ? <Badge variant="neutral">{selectedStudent.firstName} {selectedStudent.lastName}</Badge> : undefined} />
      <Card>
        <CardContent className="grid gap-3 md:grid-cols-3">
          <FormField label="Student">{({ id }) => isStudent ? <Input id={id} value={me.data ? `${me.data.student.firstName} ${me.data.student.lastName}` : 'Loading'} disabled /> : (
            <Select value={selectedStudentId} onValueChange={setSelectedStudentId}>
              <SelectTrigger id={id}><SelectValue placeholder="Select student" /></SelectTrigger>
              <SelectContent>{(students.data?.items ?? []).map((student) => <SelectItem key={student.id} value={student.id}>{student.firstName} {student.lastName}</SelectItem>)}</SelectContent>
            </Select>
          )}</FormField>
          <FormField label="Semester">{({ id }) => (
            <Select value={effectiveSemesterId ?? ''} onValueChange={setSemesterId}>
              <SelectTrigger id={id}><SelectValue placeholder="Select semester" /></SelectTrigger>
              <SelectContent>{(semesters.data ?? []).map((semester) => <SelectItem key={semester.id} value={semester.id}>{semester.name} ({semester.status})</SelectItem>)}</SelectContent>
            </Select>
          )}</FormField>
          <div className="self-end rounded-md border border-border bg-surface-secondary px-3 py-2 text-sm text-text-secondary">
            Active semester: {activeSemester.data ? `${activeSemester.data.name} ${formatDateOnly(activeSemester.data.startDate)}-${formatDateOnly(activeSemester.data.endDate)}` : <MissingData kind="not-set" />}
          </div>
        </CardContent>
      </Card>
      {!studentId ? <p className="rounded-lg border border-border bg-surface p-6 text-sm text-text-secondary">Select a student to view schedule and availability.</p> : null}
      <div className="grid gap-4 xl:grid-cols-2">
        <Card>
          <CardHeader><h2 className="text-sm font-semibold">Course schedule</h2></CardHeader>
          <CardContent className="space-y-4">
            {canManage ? <ScheduleForm studentId={studentId ?? ''} semesterId={effectiveSemesterId ?? ''} editing={editingSchedule} onDone={() => setEditingSchedule(null)} /> : null}
            {collections.schedule.isError ? <ErrorState title="Could not load schedule." description="Schedule records could not be fetched." retryAction={<Button variant="outline" onClick={() => void collections.schedule.refetch()}>Retry</Button>} /> : (
              <DataTable
                columns={scheduleColumns}
                rows={collections.schedule.data ?? []}
                getRowKey={(item) => item.id}
                isLoading={collections.schedule.isLoading}
                emptyState={<p className="text-sm text-text-secondary">No course schedule entries.</p>}
                rowActions={(item) => <div className="flex gap-1"><Button variant="ghost" size="icon" aria-label="Edit schedule entry" onClick={() => setEditingSchedule(item)}><Pencil aria-hidden="true" className="h-4 w-4" /></Button><Button variant="ghost" size="icon" aria-label="Delete schedule entry" onClick={() => mutations.deleteSchedule.mutate(item.id)}><Trash2 aria-hidden="true" className="h-4 w-4" /></Button></div>}
              />
            )}
          </CardContent>
        </Card>
        <Card>
          <CardHeader><h2 className="text-sm font-semibold">Availability</h2></CardHeader>
          <CardContent className="space-y-4">
            {canManage ? <AvailabilityForm studentId={studentId ?? ''} semesterId={effectiveSemesterId ?? ''} editing={editingAvailability} onDone={() => setEditingAvailability(null)} /> : null}
            {collections.availability.isError ? <ErrorState title="Could not load availability." description="Availability records could not be fetched." retryAction={<Button variant="outline" onClick={() => void collections.availability.refetch()}>Retry</Button>} /> : (
              <DataTable
                columns={availabilityColumns}
                rows={collections.availability.data ?? []}
                getRowKey={(item) => item.id}
                isLoading={collections.availability.isLoading}
                emptyState={<p className="text-sm text-text-secondary">No availability entries.</p>}
                rowActions={(item) => <div className="flex gap-1"><Button variant="ghost" size="icon" aria-label="Edit availability" onClick={() => setEditingAvailability(item)}><Pencil aria-hidden="true" className="h-4 w-4" /></Button><Button variant="ghost" size="icon" aria-label="Delete availability" onClick={() => mutations.deleteAvailability.mutate(item.id)}><Trash2 aria-hidden="true" className="h-4 w-4" /></Button></div>}
              />
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function ScheduleForm({ studentId, semesterId, editing, onDone }: { studentId: string; semesterId: string; editing: CourseSchedule | null; onDone: () => void }) {
  const mutations = useScheduleMutations(studentId)
  const [form, setForm] = useState({ courseName: '', courseCode: '', dayOfWeek: 'Monday' as DayOfWeek, startTime: '09:00', endTime: '10:00', location: '' })
  useEffect(() => {
    if (editing) {
      setForm({ courseName: editing.courseName, courseCode: editing.courseCode, dayOfWeek: editing.dayOfWeek, startTime: editing.startTime.slice(0, 5), endTime: editing.endTime.slice(0, 5), location: editing.location ?? '' })
    }
  }, [editing])
  const valid = useMemo(() => form.courseName.trim() && form.courseCode.trim() && isValidTimeRange(form.startTime, form.endTime), [form])
  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!valid) return
    const payload = { ...form, location: form.location || undefined }
    if (editing) {
      mutations.updateSchedule.mutate({ id: editing.id, payload }, { onSuccess: onDone })
    } else {
      mutations.createSchedule.mutate({ studentId, semesterId, ...payload }, { onSuccess: () => setForm({ ...form, courseName: '', courseCode: '', location: '' }) })
    }
  }
  return (
    <form className="grid gap-3 md:grid-cols-3" onSubmit={submit}>
      <FormField label="Course" required>{({ id }) => <Input id={id} value={form.courseName} onChange={(event) => setForm({ ...form, courseName: event.target.value })} />}</FormField>
      <FormField label="Code" required>{({ id }) => <Input id={id} value={form.courseCode} onChange={(event) => setForm({ ...form, courseCode: event.target.value })} />}</FormField>
      <FormField label="Day">{({ id }) => <Select value={form.dayOfWeek} onValueChange={(value) => setForm({ ...form, dayOfWeek: value as DayOfWeek })}><SelectTrigger id={id}><SelectValue /></SelectTrigger><SelectContent>{dayOfWeekValues.map((day) => <SelectItem key={day} value={day}>{day}</SelectItem>)}</SelectContent></Select>}</FormField>
      <FormField label="Start">{({ id }) => <Input id={id} type="time" value={form.startTime} onChange={(event) => setForm({ ...form, startTime: event.target.value })} />}</FormField>
      <FormField label="End">{({ id }) => <Input id={id} type="time" value={form.endTime} onChange={(event) => setForm({ ...form, endTime: event.target.value })} />}</FormField>
      <FormField label="Location">{({ id }) => <Input id={id} value={form.location} onChange={(event) => setForm({ ...form, location: event.target.value })} />}</FormField>
      <div className="flex gap-2">
        <Button type="submit" iconBefore={<Plus aria-hidden="true" className="h-4 w-4" />} isLoading={mutations.createSchedule.isPending || mutations.updateSchedule.isPending} disabled={!valid}>{editing ? 'Update course' : 'Add course'}</Button>
        {editing ? <Button variant="ghost" onClick={onDone}>Cancel edit</Button> : null}
      </div>
    </form>
  )
}

function AvailabilityForm({ studentId, semesterId, editing, onDone }: { studentId: string; semesterId: string; editing: Availability | null; onDone: () => void }) {
  const mutations = useScheduleMutations(studentId)
  const [form, setForm] = useState({ dayOfWeek: 'Monday' as DayOfWeek, startTime: '09:00', endTime: '12:00', status: 'AVAILABLE' as AvailabilityStatus, reason: '' })
  useEffect(() => {
    if (editing) {
      setForm({ dayOfWeek: editing.dayOfWeek, startTime: editing.startTime.slice(0, 5), endTime: editing.endTime.slice(0, 5), status: editing.status, reason: editing.reason ?? '' })
    }
  }, [editing])
  const valid = isValidTimeRange(form.startTime, form.endTime)
  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!valid) return
    const payload = { ...form, reason: form.reason || undefined }
    if (editing) {
      mutations.updateAvailability.mutate({ id: editing.id, payload }, { onSuccess: onDone })
    } else {
      mutations.createAvailability.mutate({ studentId, semesterId, ...payload }, { onSuccess: () => setForm({ ...form, reason: '' }) })
    }
  }
  return (
    <form className="grid gap-3 md:grid-cols-3" onSubmit={submit}>
      <FormField label="Day">{({ id }) => <Select value={form.dayOfWeek} onValueChange={(value) => setForm({ ...form, dayOfWeek: value as DayOfWeek })}><SelectTrigger id={id}><SelectValue /></SelectTrigger><SelectContent>{dayOfWeekValues.map((day) => <SelectItem key={day} value={day}>{day}</SelectItem>)}</SelectContent></Select>}</FormField>
      <FormField label="Start">{({ id }) => <Input id={id} type="time" value={form.startTime} onChange={(event) => setForm({ ...form, startTime: event.target.value })} />}</FormField>
      <FormField label="End">{({ id }) => <Input id={id} type="time" value={form.endTime} onChange={(event) => setForm({ ...form, endTime: event.target.value })} />}</FormField>
      <FormField label="Status">{({ id }) => <Select value={form.status} onValueChange={(value) => setForm({ ...form, status: value as AvailabilityStatus })}><SelectTrigger id={id}><SelectValue /></SelectTrigger><SelectContent>{availabilityStatuses.map((status) => <SelectItem key={status} value={status}>{status}</SelectItem>)}</SelectContent></Select>}</FormField>
      <FormField label="Reason" className="md:col-span-2">{({ id }) => <Textarea id={id} value={form.reason} onChange={(event) => setForm({ ...form, reason: event.target.value })} />}</FormField>
      <div className="flex gap-2">
        <Button type="submit" iconBefore={<Plus aria-hidden="true" className="h-4 w-4" />} isLoading={mutations.createAvailability.isPending || mutations.updateAvailability.isPending} disabled={!valid}>{editing ? 'Update availability' : 'Add availability'}</Button>
        {editing ? <Button variant="ghost" onClick={onDone}>Cancel edit</Button> : null}
      </div>
    </form>
  )
}
