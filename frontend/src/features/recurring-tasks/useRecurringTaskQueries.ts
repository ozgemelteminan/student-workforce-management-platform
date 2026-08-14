import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { activateRecurringTask, createRecurringTask, deactivateRecurringTask, deleteRecurringTask, getRecurringTask, getRecurringTasks, updateRecurringTask } from './api/recurringTasksApi'
import type { RecurringTaskFilters, RecurringTaskPayload } from './types'

export function useRecurringTasks(filters: RecurringTaskFilters) {
  return useQuery({ queryKey: queryKeys.recurringTasks.list(stableFilters(filters)), queryFn: ({ signal }) => getRecurringTasks(filters, signal) })
}

export function useRecurringTask(id: string | undefined) {
  return useQuery({ queryKey: queryKeys.recurringTasks.detail(id ?? 'missing'), queryFn: ({ signal }) => getRecurringTask(id ?? '', signal), enabled: Boolean(id) })
}

export function useRecurringTaskMutations() {
  const queryClient = useQueryClient()
  const invalidate = async (id?: string) => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.recurringTasks.all }),
      id ? queryClient.invalidateQueries({ queryKey: queryKeys.recurringTasks.detail(id) }) : Promise.resolve(),
    ])
  }
  return {
    create: useMutation({ mutationFn: (payload: Required<RecurringTaskPayload>) => createRecurringTask(payload), onSuccess: async (task) => { appToast.success('Recurring task created.'); await invalidate(task.id) } }),
    update: useMutation({ mutationFn: ({ id, payload }: { id: string; payload: Omit<RecurringTaskPayload, 'templateId'> }) => updateRecurringTask(id, payload), onSuccess: async (task) => { appToast.success('Recurring task updated.'); await invalidate(task.id) } }),
    activate: useMutation({ mutationFn: (id: string) => activateRecurringTask(id), onSuccess: async (task) => { appToast.success('Recurring task activated.'); await invalidate(task.id) } }),
    deactivate: useMutation({ mutationFn: (id: string) => deactivateRecurringTask(id), onSuccess: async (task) => { appToast.success('Recurring task deactivated.'); await invalidate(task.id) } }),
    delete: useMutation({ mutationFn: (id: string) => deleteRecurringTask(id), onSuccess: async () => { appToast.success('Recurring task deleted.'); await invalidate() } }),
  }
}

export function describeRecurrence(frequency: string, localRunTime?: string) {
  return `${frequency}${localRunTime ? ` at ${localRunTime}` : ''}`
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
