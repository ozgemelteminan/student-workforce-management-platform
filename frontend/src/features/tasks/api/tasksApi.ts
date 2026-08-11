import { apiRequest } from '../../../lib/api'
import { openSignedDownload as openTemporaryDownload, uploadSignedFile } from '../../../lib/signed-urls'
import type {
  AssignmentRecommendation,
  Category,
  Feedback,
  PaginatedResult,
  Skill,
  Student,
  Submission,
  SubmissionDownloadUrl,
  SubmissionUploadIntent,
  SubmissionVersion,
  Task,
  TaskAssignmentHistory,
  TaskChecklistItem,
  TaskComment,
  TaskCommentVisibility,
  TaskDependency,
  TaskFilters,
  TaskFormPayload,
  TaskRequiredSkill,
  UpdateTaskPayload,
} from '../types'

function params(filters: Record<string, string | number | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

function taskQuery(filters: TaskFilters) {
  return params({
    page: filters.page,
    pageSize: filters.pageSize,
    search: filters.search,
    sortBy: filters.sortBy,
    sortDirection: filters.sortDirection,
    status: filters.status,
    priority: filters.priority,
    difficulty: filters.difficulty,
    categoryId: filters.categoryId,
    studentId: filters.studentId,
    isAssigned: filters.isAssigned === undefined ? undefined : String(filters.isAssigned),
    deadlineFrom: filters.deadlineFrom,
    deadlineTo: filters.deadlineTo,
  })
}

export function getTasks(filters: TaskFilters, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<Task>>(`/tasks${taskQuery(filters)}`, { signal })
}

export function getMyTasks(filters: Pick<TaskFilters, 'page' | 'pageSize' | 'search' | 'sortBy' | 'sortDirection'>, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<Task>>(`/tasks/my${params(filters)}`, { signal })
}

export function getTask(id: string, signal?: AbortSignal) {
  return apiRequest<Task>(`/tasks/${id}`, { signal })
}

export function createTask(payload: TaskFormPayload) {
  return apiRequest<Task>('/tasks', { method: 'POST', body: payload })
}

export function updateTask(id: string, payload: UpdateTaskPayload) {
  return apiRequest<Task>(`/tasks/${id}`, { method: 'PUT', body: payload })
}

export function transitionTask(id: string, action: 'accept' | 'start' | 'submit') {
  return apiRequest<Task>(`/tasks/${id}/${action}`, { method: 'POST' })
}

export function cancelTask(id: string, reason: string) {
  return apiRequest<Task>(`/tasks/${id}/cancel`, { method: 'POST', body: { reason } })
}

export function assignTask(id: string, studentId: string, reason?: string) {
  return apiRequest<Task>(`/tasks/${id}/assign`, { method: 'POST', body: { studentId, reason } })
}

export function reassignTask(id: string, newStudentId: string, reason: string) {
  return apiRequest<Task>(`/tasks/${id}/reassign`, { method: 'POST', body: { newStudentId, reason } })
}

export function unassignTask(id: string, reason: string) {
  return apiRequest<Task>(`/tasks/${id}/unassign`, { method: 'POST', body: { reason } })
}

export function getChecklist(taskId: string, signal?: AbortSignal) {
  return apiRequest<TaskChecklistItem[]>(`/tasks/${taskId}/checklist`, { signal })
}

export function addChecklistItem(taskId: string, title: string, order: number) {
  return apiRequest<TaskChecklistItem>(`/tasks/${taskId}/checklist`, { method: 'POST', body: { title, order } })
}

export function setChecklistItem(taskId: string, itemId: string, completed: boolean) {
  return apiRequest<TaskChecklistItem>(`/tasks/${taskId}/checklist/${itemId}/${completed ? 'complete' : 'uncomplete'}`, { method: 'POST' })
}

export function updateChecklistItem(taskId: string, itemId: string, title: string) {
  return apiRequest<TaskChecklistItem>(`/tasks/${taskId}/checklist/${itemId}`, { method: 'PUT', body: { title } })
}

export function deleteChecklistItem(taskId: string, itemId: string) {
  return apiRequest<void>(`/tasks/${taskId}/checklist/${itemId}`, { method: 'DELETE' })
}

export function reorderChecklist(taskId: string, items: { checklistItemId: string; order: number }[]) {
  return apiRequest<TaskChecklistItem[]>(`/tasks/${taskId}/checklist/reorder`, { method: 'PUT', body: { items } })
}

export function getComments(taskId: string, signal?: AbortSignal) {
  return apiRequest<TaskComment[]>(`/tasks/${taskId}/comments`, { signal })
}

export function addComment(taskId: string, content: string, visibility: TaskCommentVisibility) {
  return apiRequest<TaskComment>(`/tasks/${taskId}/comments`, { method: 'POST', body: { content, visibility } })
}

export function getDependencies(taskId: string, signal?: AbortSignal) {
  return apiRequest<TaskDependency[]>(`/tasks/${taskId}/dependencies`, { signal })
}

export function addDependency(taskId: string, dependsOnTaskId: string) {
  return apiRequest<TaskDependency>(`/tasks/${taskId}/dependencies`, { method: 'POST', body: { dependsOnTaskId } })
}

export function getRequiredSkills(taskId: string, signal?: AbortSignal) {
  return apiRequest<TaskRequiredSkill[]>(`/tasks/${taskId}/skills`, { signal })
}

export function addRequiredSkill(taskId: string, skillId: string, minimumLevel: string) {
  return apiRequest<TaskRequiredSkill>(`/tasks/${taskId}/skills`, { method: 'POST', body: { skillId, minimumLevel } })
}

export function updateRequiredSkill(taskId: string, skillId: string, minimumLevel: string) {
  return apiRequest<TaskRequiredSkill>(`/tasks/${taskId}/skills/${skillId}`, { method: 'PUT', body: { minimumLevel } })
}

export function deleteRequiredSkill(taskId: string, skillId: string) {
  return apiRequest<void>(`/tasks/${taskId}/skills/${skillId}`, { method: 'DELETE' })
}

export function getAssignmentHistory(taskId: string, signal?: AbortSignal) {
  return apiRequest<TaskAssignmentHistory[]>(`/tasks/${taskId}/history`, { signal })
}

export function getRecommendations(taskId: string, signal?: AbortSignal) {
  return apiRequest<AssignmentRecommendation[]>(`/tasks/${taskId}/recommendations`, { signal })
}

export function getSubmissions(taskId: string, signal?: AbortSignal) {
  return apiRequest<Submission[]>(`/tasks/${taskId}/submissions`, { signal })
}

export function initiateSubmissionUpload(taskId: string, file: File, contentHash?: string) {
  const extension = file.name.includes('.') ? `.${file.name.split('.').pop()}` : ''
  return apiRequest<SubmissionUploadIntent>(`/tasks/${taskId}/uploads/initiate`, {
    method: 'POST',
    body: { fileName: file.name, fileSize: file.size, mimeType: file.type || 'application/octet-stream', fileExtension: extension.toLowerCase(), contentHash },
  })
}

export async function uploadSubmissionFile(taskId: string, file: File, options: { signal?: AbortSignal; onProgress?: (progress: number) => void } = {}) {
  const intent = await initiateSubmissionUpload(taskId, file)
  await uploadSignedFile(intent, file, options)
  return completeSubmissionUploadForTask(taskId, intent.submissionVersionId)
}

export function getSubmissionVersions(taskId: string, submissionId: string, signal?: AbortSignal) {
  void taskId
  return apiRequest<SubmissionVersion[]>(`/submissions/${submissionId}/versions`, { signal })
}

export function completeSubmissionUpload(versionId: string) {
  return apiRequest<SubmissionVersion>(`/submissions/versions/${versionId}/complete`, { method: 'POST' })
}

export function completeSubmissionUploadForTask(taskId: string, versionId: string) {
  return apiRequest<SubmissionVersion>(`/tasks/${taskId}/uploads/${versionId}/complete`, { method: 'POST' })
}

export function getSubmissionDownloadUrl(submissionId: string, signal?: AbortSignal) {
  return apiRequest<SubmissionDownloadUrl>(`/submissions/${submissionId}/download-url`, { signal })
}

export function getSubmissionVersionDownloadUrl(submissionId: string, versionId: string, signal?: AbortSignal) {
  return apiRequest<SubmissionDownloadUrl>(`/submissions/${submissionId}/versions/${versionId}/download-url`, { signal })
}

export function approveSubmission(submissionId: string, reviewerComment?: string) {
  return apiRequest(`/submissions/${submissionId}/approve`, { method: 'POST', body: { reviewerComment } })
}

export function requestSubmissionRevision(submissionId: string, reviewerComment: string) {
  return apiRequest(`/submissions/${submissionId}/revision-request`, { method: 'POST', body: { reviewerComment } })
}

export function getTaskFeedback(taskId: string, page = 1, pageSize = 10, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<Feedback>>(`/tasks/${taskId}/feedback${params({ page, pageSize })}`, { signal })
}

export function createTaskFeedback(taskId: string, studentId: string, rating?: number, comment?: string) {
  return apiRequest<Feedback>(`/tasks/${taskId}/feedback`, { method: 'POST', body: { studentId, rating, comment } })
}

export function getCategories(signal?: AbortSignal) {
  return apiRequest<Category[]>('/categories', { signal })
}

export function getSkills(signal?: AbortSignal) {
  return apiRequest<Skill[]>('/skills', { signal })
}

export function getStudents(search?: string, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<Student>>(`/students${params({ page: 1, pageSize: 50, search })}`, { signal })
}

export function openSignedDownload(url: string) {
  openTemporaryDownload({ downloadUrl: url })
}
