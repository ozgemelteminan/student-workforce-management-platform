import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import {
  addChecklistItem,
  addComment,
  addDependency,
  assignTask,
  cancelTask,
  completeSubmissionUpload,
  createTask,
  createTaskFeedback,
  getAssignmentHistory,
  getCategories,
  getChecklist,
  getComments,
  getDependencies,
  getMyTasks,
  getRecommendations,
  getRequiredSkills,
  getSkills,
  getStudents,
  getSubmissions,
  getSubmissionVersions,
  getTask,
  getTaskFeedback,
  getTasks,
  initiateSubmissionUpload,
  reassignTask,
  requestSubmissionRevision,
  setChecklistItem,
  transitionTask,
  unassignTask,
  updateTask,
} from './api/tasksApi'
import type { TaskFilters, TaskFormPayload, UpdateTaskPayload } from './types'

export function useTasks(filters: TaskFilters, enabled = true) {
  return useQuery({ queryKey: queryKeys.tasks.list(stableFilters(filters)), queryFn: ({ signal }) => getTasks(filters, signal), enabled })
}

export function useMyTasks(filters: Pick<TaskFilters, 'page' | 'pageSize' | 'search' | 'sortBy' | 'sortDirection'>, enabled = true) {
  return useQuery({ queryKey: queryKeys.tasks.my(stableFilters(filters)), queryFn: ({ signal }) => getMyTasks(filters, signal), enabled })
}

export function useTask(id: string | undefined) {
  return useQuery({ queryKey: queryKeys.tasks.detail(id ?? 'missing'), queryFn: ({ signal }) => getTask(id ?? '', signal), enabled: Boolean(id) })
}

export function useTaskCollections(taskId: string | undefined, staffEnabled: boolean) {
  const enabled = Boolean(taskId)
  return {
    checklist: useQuery({ queryKey: queryKeys.tasks.checklist(taskId ?? 'missing'), queryFn: ({ signal }) => getChecklist(taskId ?? '', signal), enabled }),
    comments: useQuery({ queryKey: queryKeys.tasks.comments(taskId ?? 'missing'), queryFn: ({ signal }) => getComments(taskId ?? '', signal), enabled }),
    dependencies: useQuery({ queryKey: queryKeys.tasks.dependencies(taskId ?? 'missing'), queryFn: ({ signal }) => getDependencies(taskId ?? '', signal), enabled }),
    skills: useQuery({ queryKey: queryKeys.tasks.skills(taskId ?? 'missing'), queryFn: ({ signal }) => getRequiredSkills(taskId ?? '', signal), enabled }),
    submissions: useQuery({ queryKey: queryKeys.tasks.submissions(taskId ?? 'missing'), queryFn: ({ signal }) => getSubmissions(taskId ?? '', signal), enabled }),
    history: useQuery({ queryKey: queryKeys.tasks.history(taskId ?? 'missing'), queryFn: ({ signal }) => getAssignmentHistory(taskId ?? '', signal), enabled: enabled && staffEnabled }),
    recommendations: useQuery({ queryKey: queryKeys.tasks.recommendations(taskId ?? 'missing'), queryFn: ({ signal }) => getRecommendations(taskId ?? '', signal), enabled: enabled && staffEnabled }),
    feedback: useQuery({ queryKey: queryKeys.tasks.feedback(taskId ?? 'missing'), queryFn: ({ signal }) => getTaskFeedback(taskId ?? '', 1, 10, signal), enabled }),
  }
}

export function useSubmissionVersions(taskId: string | undefined, submissionId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.tasks.submissionVersions(taskId ?? 'missing', submissionId ?? 'missing'),
    queryFn: ({ signal }) => getSubmissionVersions(taskId ?? '', submissionId ?? '', signal),
    enabled: Boolean(taskId && submissionId),
  })
}

export function useTaskLookups() {
  return {
    categories: useQuery({ queryKey: queryKeys.categories.all, queryFn: ({ signal }) => getCategories(signal) }),
    skills: useQuery({ queryKey: queryKeys.skills.all, queryFn: ({ signal }) => getSkills(signal) }),
    students: useQuery({ queryKey: queryKeys.students.list({ page: 1, pageSize: 50 }), queryFn: ({ signal }) => getStudents(undefined, signal) }),
  }
}

export function useTaskMutations(taskId?: string) {
  const queryClient = useQueryClient()
  const invalidateTask = async (id = taskId) => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.marketplace.all }),
      id ? queryClient.invalidateQueries({ queryKey: queryKeys.tasks.detail(id) }) : Promise.resolve(),
    ])
  }

  return {
    create: useMutation({
      mutationFn: (payload: TaskFormPayload) => createTask(payload),
      onSuccess: async (task) => {
        appToast.success('Task created.')
        await invalidateTask(task.id)
      },
    }),
    update: useMutation({
      mutationFn: ({ id, payload }: { id: string; payload: UpdateTaskPayload }) => updateTask(id, payload),
      onSuccess: async (task) => {
        appToast.success('Task updated.')
        await invalidateTask(task.id)
      },
    }),
    transition: useMutation({
      mutationFn: ({ id, action }: { id: string; action: 'accept' | 'start' | 'submit' }) => transitionTask(id, action),
      onSuccess: async (task) => invalidateTask(task.id),
    }),
    cancel: useMutation({ mutationFn: ({ id, reason }: { id: string; reason: string }) => cancelTask(id, reason), onSuccess: async (task) => invalidateTask(task.id) }),
    assign: useMutation({ mutationFn: ({ id, studentId, reason }: { id: string; studentId: string; reason?: string }) => assignTask(id, studentId, reason), onSuccess: async (task) => invalidateTask(task.id) }),
    reassign: useMutation({ mutationFn: ({ id, newStudentId, reason }: { id: string; newStudentId: string; reason: string }) => reassignTask(id, newStudentId, reason), onSuccess: async (task) => invalidateTask(task.id) }),
    unassign: useMutation({ mutationFn: ({ id, reason }: { id: string; reason: string }) => unassignTask(id, reason), onSuccess: async (task) => invalidateTask(task.id) }),
    addChecklist: useMutation({ mutationFn: ({ id, title, order }: { id: string; title: string; order: number }) => addChecklistItem(id, title, order), onSuccess: async (_, variables) => queryClient.invalidateQueries({ queryKey: queryKeys.tasks.checklist(variables.id) }) }),
    toggleChecklist: useMutation({ mutationFn: ({ id, itemId, completed }: { id: string; itemId: string; completed: boolean }) => setChecklistItem(id, itemId, completed), onSuccess: async (_, variables) => queryClient.invalidateQueries({ queryKey: queryKeys.tasks.checklist(variables.id) }) }),
    addComment: useMutation({ mutationFn: ({ id, content, visibility }: { id: string; content: string; visibility: 'STUDENT_VISIBLE' | 'INTERNAL' }) => addComment(id, content, visibility), onSuccess: async (_, variables) => queryClient.invalidateQueries({ queryKey: queryKeys.tasks.comments(variables.id) }) }),
    addDependency: useMutation({ mutationFn: ({ id, dependsOnTaskId }: { id: string; dependsOnTaskId: string }) => addDependency(id, dependsOnTaskId), onSuccess: async (_, variables) => queryClient.invalidateQueries({ queryKey: queryKeys.tasks.dependencies(variables.id) }) }),
    initiateUpload: useMutation({ mutationFn: ({ id, file }: { id: string; file: File }) => initiateSubmissionUpload(id, file), onSuccess: async (_, variables) => queryClient.invalidateQueries({ queryKey: queryKeys.tasks.submissions(variables.id) }) }),
    completeUpload: useMutation({ mutationFn: (versionId: string) => completeSubmissionUpload(versionId), onSuccess: async () => invalidateTask() }),
    requestRevision: useMutation({ mutationFn: ({ submissionId, comment }: { submissionId: string; comment: string }) => requestSubmissionRevision(submissionId, comment), onSuccess: async () => invalidateTask() }),
    createFeedback: useMutation({ mutationFn: ({ id, studentId, rating, comment }: { id: string; studentId: string; rating?: number; comment?: string }) => createTaskFeedback(id, studentId, rating, comment), onSuccess: async (_, variables) => queryClient.invalidateQueries({ queryKey: queryKeys.tasks.feedback(variables.id) }) }),
  }
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
