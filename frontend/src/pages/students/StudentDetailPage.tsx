import { Save, ShieldCheck, ShieldOff } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, Badge, Button, Card, CardContent, CardHeader, DataTable, Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, ErrorState, FormField, Input, MissingData, PageHeader, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, Skeleton, Textarea } from '../../components/ui'
import { TaskDeadline, TaskStatusBadge } from '../../features/tasks/components'
import { formatDuration } from '../../features/tasks/taskPresentation'
import type { Feedback, SkillLevel, Student, StudentSkillDetail, Task } from '../../features/students/types'
import { useSkillCatalog, useStudent, useStudentFeedback, useStudentMutations, useStudentSkills } from '../../features/students/useStudentQueries'
import { useTasks } from '../../features/tasks/useTaskQueries'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatIstanbulDateTime } from '../../lib/date-time'

const skillLevels: SkillLevel[] = ['BEGINNER', 'INTERMEDIATE', 'ADVANCED', 'EXPERT']

export function StudentDetailPage() {
  const { studentId } = useParams()
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('ADMIN') ?? false
  const profile = useStudent(studentId)
  const feedback = useStudentFeedback(studentId)
  const studentSkills = useStudentSkills(studentId)
  const tasks = useTasks({ page: 1, pageSize: 10, studentId, sortBy: 'deadline', sortDirection: 'asc' }, Boolean(studentId))
  const skills = useSkillCatalog()
  const mutations = useStudentMutations(studentId)
  const [editOpen, setEditOpen] = useState(false)
  const [skillId, setSkillId] = useState('')
  const [level, setLevel] = useState<SkillLevel>('BEGINNER')

  if (profile.isError) {
    return <ErrorState title="Could not load student." description="The student profile may not exist or you may not have access." retryAction={<Button variant="outline" onClick={() => void profile.refetch()}>Retry</Button>} />
  }

  const student = profile.data?.student
  const columns = [
    { key: 'title', header: 'Task', cell: (task: Task) => <div className="min-w-52"><p className="font-medium">{task.title}</p><p className="truncate text-xs text-text-muted">{task.description || 'No description'}</p></div> },
    { key: 'status', header: 'Status', cell: (task: Task) => <TaskStatusBadge status={task.status} /> },
    { key: 'deadline', header: 'Deadline', cell: (task: Task) => <TaskDeadline task={task} compact /> },
    { key: 'estimate', header: 'Estimate', cell: (task: Task) => formatDuration(task.estimatedDurationMinutes), className: 'hidden lg:table-cell' },
  ]
  const feedbackColumns = [
    { key: 'task', header: 'Task', cell: (item: Feedback) => item.taskId.slice(0, 8) },
    { key: 'rating', header: 'Rating', cell: (item: Feedback) => item.rating ?? <MissingData /> },
    { key: 'comment', header: 'Comment', cell: (item: Feedback) => item.comment || <MissingData /> },
    { key: 'created', header: 'Created', cell: (item: Feedback) => formatIstanbulDateTime(item.createdAt), className: 'hidden lg:table-cell' },
  ]

  const saveSkill = (event: FormEvent) => {
    event.preventDefault()
    if (!studentId || !skillId) return
    mutations.upsertSkill.mutate({ id: studentId, payload: { skillId, level } })
  }
  const selectedExistingSkill = studentSkills.data?.find((skill) => skill.skillId === skillId)

  return (
    <div className="space-y-5">
      <PageHeader
        title={student ? `${student.firstName} ${student.lastName}` : 'Student profile'}
        description={student?.email}
        breadcrumbs={[{ label: 'Students', href: '/students' }, { label: 'Profile' }]}
        metadata={student ? <Badge variant={student.isActive ? 'success' : 'neutral'}>{student.isActive ? 'Active' : 'Inactive'}</Badge> : undefined}
        primaryAction={student ? <Button variant="outline" onClick={() => setEditOpen(true)}>Edit profile</Button> : undefined}
        secondaryActions={student && isAdmin ? <ActivationButton student={student} /> : undefined}
      />
      <div className="grid gap-4 md:grid-cols-4">
        <Metric label="Active tasks" value={profile.data?.activeTaskCount} />
        <Metric label="Completed tasks" value={profile.data?.completedTaskCount} />
        <Metric label="Active task workload" value={profile.data ? formatDuration(profile.data.currentWorkloadMinutes) : undefined} />
        <Metric label="Weekly target" value={student ? formatWeeklyTarget(student.weeklyTargetMinutes) : undefined} />
      </div>
      <div className="grid gap-4 lg:grid-cols-[1.2fr_0.8fr]">
        <Card>
          <CardHeader><h2 className="text-sm font-semibold">Task context</h2></CardHeader>
          <CardContent>
            {tasks.isError ? <ErrorState title="Could not load task context." description="Task workload could not be fetched." retryAction={<Button variant="outline" onClick={() => void tasks.refetch()}>Retry</Button>} /> : (
              <DataTable columns={columns} rows={tasks.data?.items ?? []} getRowKey={(task) => task.id} isLoading={tasks.isLoading} emptyState={<p className="text-sm text-text-secondary">No assigned tasks.</p>} />
            )}
          </CardContent>
        </Card>
        <Card>
          <CardHeader><h2 className="text-sm font-semibold">Skills</h2></CardHeader>
          <CardContent className="space-y-4">
            <StudentSkillsList skills={studentSkills.data ?? []} isLoading={studentSkills.isLoading} isError={studentSkills.isError} onRetry={() => void studentSkills.refetch()} />
            <form className="space-y-3" onSubmit={saveSkill}>
              <FormField label="Skill">{({ id }) => (
                <Select value={skillId} disabled={skills.isError || (skills.data ?? []).length === 0} onValueChange={setSkillId}>
                  <SelectTrigger id={id}><SelectValue placeholder={skills.isLoading ? 'Loading skills' : 'Select skill'} /></SelectTrigger>
                  <SelectContent>{(skills.data ?? []).map((skill) => <SelectItem key={skill.id} value={skill.id}>{skill.name}</SelectItem>)}</SelectContent>
                </Select>
              )}</FormField>
              {skills.isError ? <p className="text-sm text-destructive">Skills could not be loaded. Refresh before saving student skills.</p> : null}
              {!skills.isLoading && !skills.isError && (skills.data ?? []).length === 0 ? <p className="text-sm text-text-secondary">No active skills are available.</p> : null}
              {selectedExistingSkill ? <p className="text-xs text-text-secondary">Saving {selectedExistingSkill.name} updates its current level.</p> : null}
              <FormField label="Level">{({ id }) => (
                <Select value={level} onValueChange={(value) => setLevel(value as SkillLevel)}>
                  <SelectTrigger id={id}><SelectValue /></SelectTrigger>
                  <SelectContent>{skillLevels.map((item) => <SelectItem key={item} value={item}>{item}</SelectItem>)}</SelectContent>
                </Select>
              )}</FormField>
              <Button type="submit" iconBefore={<Save aria-hidden="true" className="h-4 w-4" />} isLoading={mutations.upsertSkill.isPending} disabled={!skillId}>Save skill</Button>
            </form>
          </CardContent>
        </Card>
      </div>
      <Card>
        <CardHeader><h2 className="text-sm font-semibold">Feedback</h2></CardHeader>
        <CardContent>
          {feedback.isError ? <ErrorState title="Could not load feedback." description="Feedback is currently unavailable." retryAction={<Button variant="outline" onClick={() => void feedback.refetch()}>Retry</Button>} /> : (
            <DataTable columns={feedbackColumns} rows={feedback.data?.items ?? []} getRowKey={(item) => item.id} isLoading={feedback.isLoading} emptyState={<p className="text-sm text-text-secondary">No feedback recorded.</p>} />
          )}
        </CardContent>
      </Card>
      {student ? <EditStudentDialog student={student} isAdmin={isAdmin} open={editOpen} onOpenChange={setEditOpen} /> : null}
    </div>
  )
}

export function StudentSkillsList({ skills, isLoading, isError, onRetry }: { skills: StudentSkillDetail[]; isLoading: boolean; isError: boolean; onRetry: () => void }) {
  if (isLoading) {
    return <div className="space-y-2" aria-label="Loading student skills"><Skeleton className="h-8" /><Skeleton className="h-8 w-3/4" /></div>
  }

  if (isError) {
    return <ErrorState title="Could not load skills." description="The student skill list could not be fetched." retryAction={<Button variant="outline" onClick={onRetry}>Retry</Button>} />
  }

  if (skills.length === 0) {
    return <p className="rounded-md border border-border bg-surface-secondary p-3 text-sm text-text-secondary">No skills added yet.</p>
  }

  return (
    <ul className="space-y-2" aria-label="Student skills">
      {skills.map((skill) => (
        <li key={skill.skillId} className="flex items-center justify-between gap-3 rounded-md border border-border bg-surface-secondary px-3 py-2">
          <span className="min-w-0 truncate text-sm font-medium">{skill.name}</span>
          <Badge variant="info">{formatSkillLevel(skill.level)}</Badge>
        </li>
      ))}
    </ul>
  )
}

function formatSkillLevel(level: SkillLevel) {
  return level.replaceAll('_', ' ').toLowerCase().replace(/\b\w/g, (letter) => letter.toUpperCase())
}

function Metric({ label, value }: { label: string; value: string | number | undefined }) {
  return <Card><CardContent><p className="text-xs uppercase text-text-muted">{label}</p><p className="mt-2 text-2xl font-semibold">{value ?? <MissingData />}</p></CardContent></Card>
}

function ActivationButton({ student }: { student: Student }) {
  const mutations = useStudentMutations(student.id)
  const [open, setOpen] = useState(false)
  const action = student.isActive ? mutations.deactivate : mutations.activate
  return (
    <AlertDialog open={open} onOpenChange={setOpen}>
      <Button variant={student.isActive ? 'destructive' : 'outline'} iconBefore={student.isActive ? <ShieldOff aria-hidden="true" className="h-4 w-4" /> : <ShieldCheck aria-hidden="true" className="h-4 w-4" />} onClick={() => setOpen(true)}>{student.isActive ? 'Deactivate' : 'Activate'}</Button>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{student.isActive ? 'Deactivate student?' : 'Activate student?'}</AlertDialogTitle>
          <AlertDialogDescription>Deactivation is not deletion. It updates the student and linked user active state.</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction asChild><Button variant={student.isActive ? 'destructive' : 'primary'} isLoading={action.isPending} onClick={() => action.mutate(student.id)}>{student.isActive ? 'Deactivate' : 'Activate'}</Button></AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function EditStudentDialog({ student, isAdmin, open, onOpenChange }: { student: Student; isAdmin: boolean; open: boolean; onOpenChange: (open: boolean) => void }) {
  const mutations = useStudentMutations(student.id)
  const [form, setForm] = useState({ firstName: student.firstName, lastName: student.lastName, email: student.email, department: student.department, weeklyTargetMinutes: student.weeklyTargetMinutes?.toString() ?? '' })
  const submit = (event: FormEvent) => {
    event.preventDefault()
    mutations.update.mutate({
      id: student.id,
      payload: {
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        department: form.department,
        weeklyTargetMinutes: isAdmin && form.weeklyTargetMinutes !== '' ? Number(form.weeklyTargetMinutes) : isAdmin ? null : undefined,
      },
    }, { onSuccess: () => onOpenChange(false) })
  }
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader><DialogTitle>Edit student profile</DialogTitle></DialogHeader>
        <form className="space-y-3" onSubmit={submit}>
          <FormField label="First name" required>{({ id }) => <Input id={id} value={form.firstName} onChange={(event) => setForm({ ...form, firstName: event.target.value })} />}</FormField>
          <FormField label="Last name" required>{({ id }) => <Input id={id} value={form.lastName} onChange={(event) => setForm({ ...form, lastName: event.target.value })} />}</FormField>
          <FormField label="Email" required>{({ id }) => <Input id={id} type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} />}</FormField>
          <FormField label="Department" required>{({ id }) => <Input id={id} value={form.department} onChange={(event) => setForm({ ...form, department: event.target.value })} />}</FormField>
          {isAdmin ? <FormField label="Weekly target minutes" helperText="Leave empty to keep the weekly target not configured.">{({ id }) => <Input id={id} type="number" min={0} value={form.weeklyTargetMinutes} onChange={(event) => setForm({ ...form, weeklyTargetMinutes: event.target.value })} />}</FormField> : null}
          <FormField label="Notes" helperText="Profile notes are not part of the current write contract.">{({ id }) => <Textarea id={id} value="" disabled placeholder="Not available yet" />}</FormField>
          <DialogFooter><Button type="submit" isLoading={mutations.update.isPending}>Save changes</Button></DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

export function formatWeeklyTarget(value: number | null | undefined) {
  return value === null || value === undefined ? 'Not configured' : formatDuration(value)
}
