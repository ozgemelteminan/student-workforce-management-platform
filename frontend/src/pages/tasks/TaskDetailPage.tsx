import { ArrowDown, ArrowUp, Download, MessageSquarePlus, Paperclip, Play, Send, Trash2, UserPlus } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { Badge, Button, Card, CardContent, CardHeader, Checkbox, EmptyState, ErrorState, FormField, Input, PageHeader, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, Tabs, TabsContent, TabsList, TabsTrigger, Textarea } from '../../components/ui'
import { ApiError } from '../../lib/api'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatIstanbulDateTime } from '../../lib/date-time'
import { appToast } from '../../lib/toast'
import { ConflictMessage, TaskDeadline, TaskPriorityBadge, TaskStatusBadge, TaskSummary } from '../../features/tasks/components'
import { formatDuration } from '../../features/tasks/taskPresentation'
import { useSubmissionVersions, useTask, useTaskCollections, useTaskLookups, useTaskMutations } from '../../features/tasks/useTaskQueries'
import type { Submission, Task, TaskCommentVisibility } from '../../features/tasks/types'

const oneGb = 1024 * 1024 * 1024

export function TaskDetailPage() {
  const { taskId } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const roles = user?.roles ?? []
  const staff = roles.includes('ADMIN') || roles.includes('TASK_MANAGER')
  const reviewer = roles.includes('ADMIN') || roles.includes('REVIEWER')
  const student = roles.includes('STUDENT')
  const task = useTask(taskId)
  const collections = useTaskCollections(taskId, staff)
  const lookups = useTaskLookups()
  const mutations = useTaskMutations(taskId)
  const [comment, setComment] = useState('')
  const [commentVisibility, setCommentVisibility] = useState<TaskCommentVisibility>('STUDENT_VISIBLE')
  const [checklistTitle, setChecklistTitle] = useState('')
  const [dependencyId, setDependencyId] = useState('')
  const [editingChecklistId, setEditingChecklistId] = useState<string | null>(null)
  const [editingChecklistTitle, setEditingChecklistTitle] = useState('')
  const [selectedStudentId, setSelectedStudentId] = useState('')
  const [reason, setReason] = useState('')
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [uploadProgress, setUploadProgress] = useState<number | null>(null)
  const [uploadController, setUploadController] = useState<AbortController | null>(null)
  const [conflict, setConflict] = useState(false)
  const selectedSubmission = collections.submissions.data?.[0]
  const versions = useSubmissionVersions(taskId, selectedSubmission?.id)

  const taskData = task.data
  const canTransition = useMemo(() => taskData ? visibleTransitions(taskData, roles) : [], [roles, taskData])
  const primaryTransition = canTransition[0]

  const runMutation = async (work: () => Promise<unknown>) => {
    setConflict(false)
    try {
      await work()
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setConflict(true)
        void task.refetch()
        return
      }
      throw error
    }
  }

  if (task.isLoading) return <PageHeader title="Task detail" description="Loading task workspace." />
  if (task.isError) return <ErrorState title="Task not available." description="The task could not be loaded or you do not have access." retryAction={<Button variant="outline" onClick={() => void task.refetch()}>Retry</Button>} navigationAction={<Button variant="outline" onClick={() => navigate('/tasks')}>Back to tasks</Button>} />
  if (!taskData) return null

  const submitFile = async (file: File | undefined) => {
    setUploadError(null)
    if (!file) return
    if (file.size > oneGb) {
      setUploadError('Files must be 1 GB or smaller.')
      return
    }
    const controller = new AbortController()
    setUploadController(controller)
    setUploadProgress(0)
    try {
      await mutations.initiateUpload.mutateAsync({ id: taskData.id, file, signal: controller.signal, onProgress: setUploadProgress })
      appToast.success('Submission uploaded.')
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') appToast.info('Upload cancelled.')
      else throw error
    } finally {
      setUploadController(null)
      setUploadProgress(null)
    }
  }

  const moveChecklist = (index: number, direction: -1 | 1) => {
    const items = collections.checklist.data ?? []
    const target = index + direction
    if (target < 0 || target >= items.length) return
    const reordered = [...items]
    const [item] = reordered.splice(index, 1)
    if (!item) return
    reordered.splice(target, 0, item)
    void runMutation(() => mutations.reorderChecklist.mutateAsync({ id: taskData.id, items: reordered.map((entry, order) => ({ checklistItemId: entry.id, order })) }))
  }

  return (
    <div className="space-y-5">
      <PageHeader
        title={taskData.title}
        description="Task Detail composes the confirmed task, comments, checklist, dependencies, submissions, history, recommendations, and feedback endpoints."
        breadcrumbs={[{ label: 'Tasks', href: '/tasks' }, { label: taskData.title }]}
        metadata={<><TaskStatusBadge status={taskData.status} /><TaskPriorityBadge priority={taskData.priority} /><TaskDeadline task={taskData} compact /></>}
        primaryAction={primaryTransition ? <Button isLoading={mutations.transition.isPending} iconBefore={<Play className="h-4 w-4" />} onClick={() => void runMutation(() => mutations.transition.mutateAsync({ id: taskData.id, action: primaryTransition.action }))}>{primaryTransition.label}</Button> : undefined}
        secondaryActions={staff ? <Button variant="outline" onClick={() => navigate(`/tasks/${taskData.id}/edit`)}>Edit</Button> : undefined}
      />
      {conflict ? <ConflictMessage onReload={() => void task.refetch()} /> : null}
      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="space-y-5">
          <TaskSummary task={taskData} checklist={collections.checklist.data} />
          <Tabs defaultValue="overview">
            <TabsList>
              <TabsTrigger value="overview">Overview</TabsTrigger>
              <TabsTrigger value="submissions">Submissions</TabsTrigger>
              <TabsTrigger value="activity">Activity</TabsTrigger>
              <TabsTrigger value="feedback">Feedback</TabsTrigger>
            </TabsList>
            <TabsContent value="overview" className="space-y-5">
              <Panel title="Checklist" error={collections.checklist.isError} onRetry={() => void collections.checklist.refetch()}>
                {collections.checklist.data?.length === 0 ? <EmptyState title="No checklist items." description={staff ? 'Add a supported checklist item below.' : 'No checklist has been defined for this task.'} /> : null}
                <div className="space-y-2">
                  {collections.checklist.data?.map((item, index) => (
                    <div key={item.id} className="flex flex-wrap items-center gap-2 rounded-md border border-border px-3 py-2 text-sm">
                      <Checkbox checked={item.isCompleted} onCheckedChange={(checked) => void mutations.toggleChecklist.mutate({ id: taskData.id, itemId: item.id, completed: checked === true })} />
                      {editingChecklistId === item.id ? <Input aria-label="Checklist item title" className="min-w-48 flex-1" value={editingChecklistTitle} onChange={(event) => setEditingChecklistTitle(event.target.value)} /> : <span className={item.isCompleted ? 'min-w-48 flex-1 text-text-muted line-through' : 'min-w-48 flex-1 text-text-primary'}>{item.title}</span>}
                      {staff ? <div className="ml-auto flex items-center gap-1">
                        {editingChecklistId === item.id ? <Button type="button" size="sm" onClick={() => void runMutation(() => mutations.updateChecklist.mutateAsync({ id: taskData.id, itemId: item.id, title: editingChecklistTitle })).then(() => setEditingChecklistId(null))}>Save</Button> : <Button type="button" size="sm" variant="ghost" onClick={() => { setEditingChecklistId(item.id); setEditingChecklistTitle(item.title) }}>Edit</Button>}
                        <Button type="button" size="icon" variant="ghost" aria-label="Move checklist item up" onClick={() => moveChecklist(index, -1)} disabled={index === 0}><ArrowUp className="h-4 w-4" /></Button>
                        <Button type="button" size="icon" variant="ghost" aria-label="Move checklist item down" onClick={() => moveChecklist(index, 1)} disabled={index === (collections.checklist.data?.length ?? 0) - 1}><ArrowDown className="h-4 w-4" /></Button>
                        <Button type="button" size="icon" variant="ghost" aria-label="Delete checklist item" onClick={() => { if (window.confirm('Delete this checklist item?')) void runMutation(() => mutations.deleteChecklist.mutateAsync({ id: taskData.id, itemId: item.id })) }}><Trash2 className="h-4 w-4" /></Button>
                      </div> : null}
                    </div>
                  ))}
                </div>
                {staff ? <form className="mt-3 flex gap-2" onSubmit={(event) => { event.preventDefault(); if (checklistTitle.trim()) void mutations.addChecklist.mutateAsync({ id: taskData.id, title: checklistTitle, order: collections.checklist.data?.length ?? 0 }).then(() => setChecklistTitle('')) }}><Input aria-label="New checklist item" value={checklistTitle} onChange={(event) => setChecklistTitle(event.target.value)} /><Button type="submit" isLoading={mutations.addChecklist.isPending}>Add</Button></form> : null}
              </Panel>
              <Panel title="Comments" error={collections.comments.isError} onRetry={() => void collections.comments.refetch()}>
                {collections.comments.data?.length === 0 ? <EmptyState title="No comments yet." /> : null}
                <div className="space-y-3">
                  {collections.comments.data?.map((item) => <article key={item.id} className="rounded-md border border-border px-3 py-3"><div className="flex justify-between gap-3 text-xs text-text-muted"><span>{item.visibility}</span><time>{formatIstanbulDateTime(item.createdAt)}</time></div><p className="mt-2 whitespace-pre-wrap text-sm">{item.content}</p></article>)}
                </div>
                <form className="mt-3 space-y-2" onSubmit={(event) => { event.preventDefault(); if (comment.trim()) void mutations.addComment.mutateAsync({ id: taskData.id, content: comment, visibility: student ? 'STUDENT_VISIBLE' : commentVisibility }).then(() => setComment('')) }}>
                  {!student ? <Select value={commentVisibility} onValueChange={(value) => setCommentVisibility(value as TaskCommentVisibility)}><SelectTrigger aria-label="Comment visibility"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="STUDENT_VISIBLE">Student visible</SelectItem><SelectItem value="INTERNAL">Internal</SelectItem></SelectContent></Select> : null}
                  <Textarea aria-label="Comment" value={comment} onChange={(event) => setComment(event.target.value)} />
                  <Button type="submit" isLoading={mutations.addComment.isPending} iconBefore={<MessageSquarePlus className="h-4 w-4" />}>Add comment</Button>
                </form>
              </Panel>
            </TabsContent>
            <TabsContent value="submissions" className="space-y-5">
              <SubmissionsPanel task={taskData} submissions={collections.submissions.data} versions={versions.data} selectedSubmission={selectedSubmission} reviewer={reviewer} student={student} uploadError={uploadError} uploadProgress={uploadProgress} onFile={submitFile} onCancelUpload={() => uploadController?.abort()} onDownload={(submissionId, versionId) => mutations.downloadVersion.mutate({ submissionId, versionId })} onRevision={(submissionId, reviewerComment) => runMutation(() => mutations.requestRevision.mutateAsync({ submissionId, comment: reviewerComment }))} pending={mutations.initiateUpload.isPending || mutations.requestRevision.isPending || mutations.downloadVersion.isPending} />
            </TabsContent>
            <TabsContent value="activity" className="space-y-5">
              <Panel title="Assignment history" error={collections.history.isError} onRetry={() => void collections.history.refetch()}>
                {!staff ? <EmptyState title="Assignment history is staff-only." /> : null}
                {staff && collections.history.data?.length === 0 ? <EmptyState title="No assignment history." /> : null}
                <div className="space-y-2">{collections.history.data?.map((item) => <div key={item.id} className="rounded-md border border-border px-3 py-2 text-sm"><p className="font-medium">{item.mode} · {item.status}</p><p className="text-text-secondary">{formatIstanbulDateTime(item.assignedAt)} · student {item.studentId.slice(0, 8)}</p>{item.reason ? <p className="mt-1 text-text-secondary">{item.reason}</p> : null}</div>)}</div>
              </Panel>
            </TabsContent>
            <TabsContent value="feedback">
              <Panel title="Feedback" error={collections.feedback.isError} onRetry={() => void collections.feedback.refetch()}>
                {collections.feedback.data?.items.length === 0 ? <EmptyState title="No feedback recorded." /> : null}
                <div className="space-y-2">{collections.feedback.data?.items.map((item) => <div key={item.id} className="rounded-md border border-border px-3 py-2 text-sm"><p className="font-medium">{item.rating ? `${item.rating}/5` : 'Comment'}</p><p className="text-text-secondary">{item.comment || '—'}</p></div>)}</div>
              </Panel>
            </TabsContent>
          </Tabs>
        </div>
        <aside className="space-y-5">
          <Panel title="Assignment">
            <dl className="space-y-2 text-sm"><Row label="Assigned student" value={taskData.assignedStudentId ?? 'Not set'} /><Row label="Estimate" value={formatDuration(taskData.estimatedDurationMinutes)} /><Row label="Category" value={taskData.categoryId} /></dl>
            {staff ? <form className="mt-3 space-y-2" onSubmit={(event) => { event.preventDefault(); if (selectedStudentId) void runMutation(() => (taskData.assignedStudentId ? mutations.reassign.mutateAsync({ id: taskData.id, newStudentId: selectedStudentId, reason: reason || 'Updated assignment' }) : mutations.assign.mutateAsync({ id: taskData.id, studentId: selectedStudentId, reason }))) }}><Select value={selectedStudentId} onValueChange={setSelectedStudentId}><SelectTrigger aria-label="Assign student"><SelectValue placeholder={lookups.students.isLoading ? 'Loading students' : 'Select student'} /></SelectTrigger><SelectContent>{lookups.students.data?.items.map((student) => <SelectItem key={student.id} value={student.id}>{student.firstName} {student.lastName}</SelectItem>)}</SelectContent></Select><Input aria-label="Assignment reason" value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Reason" /><Button type="submit" className="w-full" isLoading={mutations.assign.isPending || mutations.reassign.isPending} iconBefore={<UserPlus className="h-4 w-4" />}>{taskData.assignedStudentId ? 'Reassign' : 'Assign'}</Button></form> : null}
          </Panel>
          <Panel title="Required skills" error={collections.skills.isError} onRetry={() => void collections.skills.refetch()}>
            {collections.skills.data?.length === 0 ? <EmptyState title="No required skills." /> : null}
            <div className="flex flex-wrap gap-2">{collections.skills.data?.map((skill) => <Badge key={skill.id} variant="neutral">{skill.skillName ?? skill.skillId.slice(0, 8)} · {skill.minimumLevel}</Badge>)}</div>
          </Panel>
          <Panel title="Dependencies" error={collections.dependencies.isError} onRetry={() => void collections.dependencies.refetch()}>
            {collections.dependencies.data?.length === 0 ? <EmptyState title="No dependencies." /> : null}
            <div className="space-y-2">{collections.dependencies.data?.map((item) => <Link key={item.id} className="block rounded-md border border-border px-3 py-2 text-sm hover:bg-surface-secondary" to={`/tasks/${item.dependsOnTaskId}`}>Depends on {item.dependsOnTaskId.slice(0, 8)}</Link>)}</div>
            {staff ? <form className="mt-3 flex gap-2" onSubmit={(event) => { event.preventDefault(); if (dependencyId.trim()) void mutations.addDependency.mutateAsync({ id: taskData.id, dependsOnTaskId: dependencyId.trim() }).then(() => setDependencyId('')) }}><Input aria-label="Dependency task id" value={dependencyId} onChange={(event) => setDependencyId(event.target.value)} placeholder="Task id" /><Button type="submit">Add</Button></form> : null}
          </Panel>
          <Panel title="Recommendations" error={collections.recommendations.isError} onRetry={() => void collections.recommendations.refetch()}>
            {!staff ? <EmptyState title="Recommendations are staff-only." /> : null}
            <div className="space-y-2">{collections.recommendations.data?.slice(0, 5).map((item) => <div key={item.studentId} className="rounded-md border border-border px-3 py-2 text-sm"><p className="font-medium">{item.studentName} · {item.score}</p><p className="text-text-secondary">{item.reasons.join(', ') || 'No reasons returned.'}</p></div>)}</div>
          </Panel>
        </aside>
      </div>
    </div>
  )
}

function Panel({ title, children, error, onRetry }: { title: string; children: React.ReactNode; error?: boolean; onRetry?: () => void }) {
  return <Card><CardHeader><h2 className="text-sm font-semibold">{title}</h2></CardHeader><CardContent>{error ? <ErrorState title={`Could not load ${title.toLowerCase()}.`} description="The rest of the task workspace remains available." retryAction={<Button variant="outline" onClick={onRetry}>Retry</Button>} /> : children}</CardContent></Card>
}

function Row({ label, value }: { label: string; value: string }) {
  return <div className="flex justify-between gap-3"><dt className="text-text-muted">{label}</dt><dd className="truncate font-medium text-text-primary">{value}</dd></div>
}

function SubmissionsPanel({ task, submissions, versions, selectedSubmission, reviewer, student, uploadError, uploadProgress, onFile, onCancelUpload, onDownload, onRevision, pending }: { task: Task; submissions?: Submission[]; versions?: { id: string; versionNumber: number; fileName: string; fileSize: number; fileStatus: string; uploadedAt: string }[]; selectedSubmission?: Submission; reviewer: boolean; student: boolean; uploadError: string | null; uploadProgress: number | null; onFile: (file: File | undefined) => Promise<void>; onCancelUpload: () => void; onDownload: (submissionId: string, versionId: string) => void; onRevision: (submissionId: string, comment: string) => Promise<unknown>; pending: boolean }) {
  const [reviewComment, setReviewComment] = useState('')
  return (
    <Panel title="Submissions">
      {submissions?.length === 0 ? <EmptyState title="No submissions yet." description={student ? 'Upload is available for assigned task work when backend storage is configured.' : 'No student submission has been recorded.'} /> : null}
      <div className="space-y-3">{submissions?.map((submission) => <div key={submission.id} className="rounded-md border border-border px-3 py-3 text-sm"><div className="flex flex-wrap items-center justify-between gap-2"><p className="font-medium">Submission {submission.id.slice(0, 8)}</p><Badge variant={submission.status === 'APPROVED' ? 'success' : submission.status === 'REVISION_REQUESTED' ? 'warning' : 'info'}>{submission.status}</Badge></div><p className="mt-1 text-text-secondary">{submission.submittedAt ? formatIstanbulDateTime(submission.submittedAt) : 'Not submitted yet'}</p></div>)}</div>
      {selectedSubmission ? <div className="mt-4 space-y-2"><h3 className="text-sm font-semibold">Versions</h3>{versions?.map((version) => <div key={version.id} className="rounded-md border border-border px-3 py-2 text-sm"><div className="flex flex-wrap items-center justify-between gap-2"><p className="font-medium">v{version.versionNumber} · {version.fileName}</p>{version.fileStatus === 'CONFIRMED' ? <Button type="button" size="sm" variant="outline" iconBefore={<Download className="h-4 w-4" />} onClick={() => onDownload(selectedSubmission.id, version.id)}>Download</Button> : null}</div><p className="text-text-secondary">{version.fileStatus} · {Math.ceil(version.fileSize / 1024)} KB · {formatIstanbulDateTime(version.uploadedAt)}</p></div>)}</div> : null}
      {student && task.status !== 'COMPLETED' && task.status !== 'CANCELLED' ? <div className="mt-4 space-y-2"><FormField label="Submission file" error={uploadError ?? undefined} helperText="Files must be 1 GB or smaller. Uploads use the signed direct storage flow.">{({ id, describedBy, invalid }) => <Input id={id} type="file" invalid={invalid} aria-describedby={describedBy} onChange={(event) => void onFile(event.currentTarget.files?.[0])} />}</FormField>{uploadProgress !== null ? <div className="flex items-center gap-3"><progress className="h-2 flex-1" max={100} value={uploadProgress} aria-label="Upload progress" /><Button type="button" size="sm" variant="outline" onClick={onCancelUpload}>Cancel upload</Button></div> : null}</div> : null}
      {reviewer && selectedSubmission?.status === 'SUBMITTED_FOR_REVIEW' ? <form className="mt-4 space-y-2" onSubmit={(event) => { event.preventDefault(); if (selectedSubmission && reviewComment.trim()) void onRevision(selectedSubmission.id, reviewComment) }}><Textarea aria-label="Revision comment" value={reviewComment} onChange={(event) => setReviewComment(event.target.value)} /><Button type="submit" isLoading={pending} variant="outline" iconBefore={<Send className="h-4 w-4" />}>Request revision</Button></form> : null}
      <p className="mt-3 flex items-start gap-2 text-xs text-text-muted"><Paperclip aria-hidden="true" className="mt-0.5 h-3.5 w-3.5" /> Download links request fresh temporary signed URLs on demand.</p>
    </Panel>
  )
}

function visibleTransitions(task: Task, roles: readonly string[]): { action: 'accept' | 'start' | 'submit'; label: string }[] {
  const student = roles.includes('STUDENT')
  if (!student) return []
  if (task.status === 'ASSIGNED') return [{ action: 'accept', label: 'Accept task' }]
  if (task.status === 'ACCEPTED') return [{ action: 'start', label: 'Start task' }]
  if (task.status === 'IN_PROGRESS') return [{ action: 'submit', label: 'Submit for review' }]
  return []
}
