import { Download, Eye, FileCheck2, RotateCcw } from 'lucide-react'
import { useMemo, useState, type FormEvent } from 'react'
import { Badge, Button, Card, CardContent, CardHeader, DataTable, ErrorState, FormField, MissingData, PageHeader, Textarea } from '../../components/ui'
import type { ReviewQueueItem, SubmissionVersion } from '../../features/reviews/types'
import { useReviewMutations, useReviewQueue, useReviewVersions } from '../../features/reviews/useReviewQueries'
import { TaskDeadline, TaskPriorityBadge, TaskStatusBadge } from '../../features/tasks/components'
import { fileStatusLabels, fileStatusVariant, formatDuration } from '../../features/tasks/taskPresentation'
import { formatIstanbulDateTime } from '../../lib/date-time'

export function ReviewsPage() {
  const queue = useReviewQueue()
  const [selected, setSelected] = useState<ReviewQueueItem | null>(null)
  const submission = useMemo(() => selected?.submissions.find((item) => item.status === 'SUBMITTED_FOR_REVIEW') ?? null, [selected])
  const versions = useReviewVersions(selected?.task.id, submission?.id)
  const mutations = useReviewMutations()
  const [comment, setComment] = useState('')

  const columns = [
    { key: 'task', header: 'Task', cell: (item: ReviewQueueItem) => <div className="min-w-52"><p className="font-medium">{item.task.title}</p><p className="text-xs text-text-muted">{item.task.id.slice(0, 8)}</p></div> },
    { key: 'status', header: 'Status', cell: (item: ReviewQueueItem) => <TaskStatusBadge status={item.task.status} /> },
    { key: 'priority', header: 'Priority', cell: (item: ReviewQueueItem) => <TaskPriorityBadge priority={item.task.priority} /> },
    { key: 'deadline', header: 'Deadline', cell: (item: ReviewQueueItem) => <TaskDeadline task={item.task} compact /> },
    { key: 'estimate', header: 'Estimate', cell: (item: ReviewQueueItem) => formatDuration(item.task.estimatedDurationMinutes), className: 'hidden lg:table-cell' },
  ]
  const versionColumns = [
    { key: 'version', header: 'Version', cell: (item: SubmissionVersion) => <Badge variant="neutral">v{item.versionNumber}</Badge> },
    { key: 'file', header: 'File', cell: (item: SubmissionVersion) => <div className="min-w-48"><p className="font-medium">{item.fileName}</p><p className="text-xs text-text-muted">{item.fileExtension} · {formatFileSize(item.fileSize)}</p></div> },
    { key: 'status', header: 'Status', cell: (item: SubmissionVersion) => <Badge variant={fileStatusVariant(item.fileStatus)}>{fileStatusLabels[item.fileStatus]}</Badge> },
    { key: 'uploaded', header: 'Uploaded', cell: (item: SubmissionVersion) => formatIstanbulDateTime(item.uploadedAt), className: 'hidden lg:table-cell' },
  ]

  const approve = (event: FormEvent) => {
    event.preventDefault()
    if (!submission) return
    mutations.approve.mutate({ submissionId: submission.id, reviewerComment: comment || undefined, taskId: selected?.task.id }, { onSuccess: resetReviewSelection })
  }

  const revision = (event: FormEvent) => {
    event.preventDefault()
    if (!submission || !comment.trim()) return
    mutations.requestRevision.mutate({ submissionId: submission.id, reviewerComment: comment.trim(), taskId: selected?.task.id }, { onSuccess: resetReviewSelection })
  }

  const selectQueueItem = (item: ReviewQueueItem) => {
    setSelected(item)
    setComment('')
  }

  const resetReviewSelection = () => {
    setSelected(null)
    setComment('')
  }

  return (
    <div className="space-y-5">
      <PageHeader title="Reviews" description="Review submitted task work from the task/submission contract. Revision is a submission outcome, not a task state." metadata={<Badge variant="neutral">{queue.data?.length ?? 0} review candidates</Badge>} />
      <div className="grid min-w-0 gap-4 xl:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]" data-testid="reviews-layout-grid">
        <Card>
          <CardHeader><h2 className="text-sm font-semibold">Review queue</h2></CardHeader>
          <CardContent>
            {queue.isError ? <ErrorState title="Could not load review queue." description="Submitted tasks or submissions could not be fetched." retryAction={<Button variant="outline" onClick={() => void queue.refetch()}>Retry</Button>} /> : (
              <DataTable
                columns={columns}
                rows={queue.data ?? []}
                getRowKey={(item) => item.task.id}
                isLoading={queue.isLoading}
                emptyState={<p className="text-sm text-text-secondary">No submissions are waiting for review.</p>}
                onRowClick={selectQueueItem}
              />
            )}
          </CardContent>
        </Card>
        <Card>
          <CardHeader><h2 className="text-sm font-semibold">Submission review</h2></CardHeader>
          <CardContent className="space-y-4">
            {selected ? (
              <>
                <div className="rounded-md border border-border bg-surface-secondary p-3">
                  <p className="font-medium">{selected.task.title}</p>
                  <p className="mt-1 text-sm text-text-secondary">Submission: {submission?.id.slice(0, 8) ?? <MissingData />}</p>
                </div>
                <DataTable
                  columns={versionColumns}
                  rows={versions.data ?? []}
                  getRowKey={(item) => item.id}
                  isLoading={versions.isLoading}
                  emptyState={<p className="text-sm text-text-secondary">No submission versions available.</p>}
                  rowActions={(item) => item.fileStatus === 'CONFIRMED' ? <div className="flex gap-1"><Button variant="ghost" size="icon" aria-label="View submission version" onClick={() => submission && mutations.viewVersion.mutate({ submissionId: submission.id, versionId: item.id })}><Eye aria-hidden="true" className="h-4 w-4" /></Button><Button variant="ghost" size="icon" aria-label="Download submission version" onClick={() => submission && mutations.downloadVersion.mutate({ submissionId: submission.id, versionId: item.id })}><Download aria-hidden="true" className="h-4 w-4" /></Button></div> : null}
                />
                <form className="space-y-3" onSubmit={approve}>
                  <FormField label="Reviewer comment">{({ id }) => <Textarea id={id} value={comment} onChange={(event) => setComment(event.target.value)} />}</FormField>
                  <div className="flex flex-wrap gap-2">
                    <Button type="submit" iconBefore={<FileCheck2 aria-hidden="true" className="h-4 w-4" />} isLoading={mutations.approve.isPending}>Approve</Button>
                    <Button type="button" variant="outline" iconBefore={<RotateCcw aria-hidden="true" className="h-4 w-4" />} isLoading={mutations.requestRevision.isPending} disabled={!comment.trim()} onClick={revision}>Request revision</Button>
                  </div>
                </form>
              </>
            ) : <p className="text-sm text-text-secondary">Select a submitted task to inspect versions and review the latest submission.</p>}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function formatFileSize(size: number) {
  if (size < 1024) return `${size} B`
  if (size < 1024 * 1024) return `${Math.round(size / 1024)} KB`
  return `${Math.round(size / 1024 / 1024)} MB`
}
