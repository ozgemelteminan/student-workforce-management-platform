import { getSubmissionVersionDownloadUrl, getSubmissionVersions, getSubmissions, getTasks, openSignedDownload, openSignedView } from '../../tasks/api/tasksApi'
import { apiRequest } from '../../../lib/api'
import type { ReviewQueueItem, TaskReview } from '../types'

export async function getReviewQueue(signal?: AbortSignal): Promise<ReviewQueueItem[]> {
  const tasks = await getTasks({ page: 1, pageSize: 50, status: 'SUBMITTED_FOR_REVIEW', sortBy: 'deadline', sortDirection: 'asc' }, signal)
  const withSubmissions = await Promise.all(tasks.items.map(async (task) => ({ task, submissions: await getSubmissions(task.id, signal) })))
  return withSubmissions.filter((item) => item.submissions.length > 0)
}

export function approveSubmission(submissionId: string, reviewerComment?: string) {
  return apiRequest<TaskReview>(`/submissions/${submissionId}/approve`, { method: 'POST', body: { reviewerComment } })
}

export function requestSubmissionRevision(submissionId: string, reviewerComment: string) {
  return apiRequest<TaskReview>(`/submissions/${submissionId}/revision-request`, { method: 'POST', body: { reviewerComment } })
}

export function getVersions(taskId: string, submissionId: string, signal?: AbortSignal) {
  return getSubmissionVersions(taskId, submissionId, signal)
}

export async function openSubmissionVersionDownload(submissionId: string, versionId: string) {
  const target = await getSubmissionVersionDownloadUrl(submissionId, versionId)
  openSignedDownload(target.signedDownloadUrl, target.fileName)
}

export async function openSubmissionVersionView(submissionId: string, versionId: string) {
  const target = await getSubmissionVersionDownloadUrl(submissionId, versionId)
  openSignedView(target.signedDownloadUrl)
}
