import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { createCategory, deactivateCategory, getCategories, reactivateCategory, updateCategory } from './api/categoriesApi'
import type { ReferenceDataPayload } from './types'

export function useCategories(includeInactive = false) {
  return useQuery({ queryKey: queryKeys.categories.list({ includeInactive }), queryFn: ({ signal }) => getCategories(includeInactive, signal) })
}

export function useCategoryMutations() {
  const queryClient = useQueryClient()
  const invalidate = async () => queryClient.invalidateQueries({ queryKey: queryKeys.categories.all })
  return {
    create: useMutation({ mutationFn: (payload: ReferenceDataPayload) => createCategory(payload), onSuccess: async () => { appToast.success('Category created.'); await invalidate() } }),
    update: useMutation({ mutationFn: ({ id, payload }: { id: string; payload: ReferenceDataPayload }) => updateCategory(id, payload), onSuccess: async () => { appToast.success('Category updated.'); await invalidate() } }),
    deactivate: useMutation({ mutationFn: (id: string) => deactivateCategory(id), onSuccess: async () => { appToast.success('Category deactivated.'); await invalidate() } }),
    reactivate: useMutation({ mutationFn: (id: string) => reactivateCategory(id), onSuccess: async () => { appToast.success('Category reactivated.'); await invalidate() } }),
  }
}
