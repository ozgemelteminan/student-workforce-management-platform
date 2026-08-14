import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { createTaskFromTemplate, createTemplate, deleteTemplate, getTemplate, getTemplates, updateTemplate } from './api/templatesApi'
import type { CreateTaskFromTemplatePayload, TemplateFilters, TemplatePayload } from './types'

export function useTemplates(filters: TemplateFilters) {
  return useQuery({ queryKey: queryKeys.templates.list(stableFilters(filters)), queryFn: ({ signal }) => getTemplates(filters, signal) })
}

export function useTemplate(id: string | undefined) {
  return useQuery({ queryKey: queryKeys.templates.detail(id ?? 'missing'), queryFn: ({ signal }) => getTemplate(id ?? '', signal), enabled: Boolean(id) })
}

export function useTemplateMutations() {
  const queryClient = useQueryClient()
  const invalidate = async (id?: string) => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.templates.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.recurringTasks.all }),
      id ? queryClient.invalidateQueries({ queryKey: queryKeys.templates.detail(id) }) : Promise.resolve(),
    ])
  }

  return {
    create: useMutation({ mutationFn: (payload: TemplatePayload) => createTemplate(payload), onSuccess: async (template) => { appToast.success('Template created.'); await invalidate(template.id) } }),
    update: useMutation({ mutationFn: ({ id, payload }: { id: string; payload: TemplatePayload }) => updateTemplate(id, payload), onSuccess: async (template) => { appToast.success('Template updated.'); await invalidate(template.id) } }),
    delete: useMutation({ mutationFn: (id: string) => deleteTemplate(id), onSuccess: async () => { appToast.success('Template deleted.'); await invalidate() } }),
    createTask: useMutation({ mutationFn: ({ id, payload }: { id: string; payload: CreateTaskFromTemplatePayload }) => createTaskFromTemplate(id, payload), onSuccess: async () => { appToast.success('Task created from template.'); await queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all }) } }),
  }
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
