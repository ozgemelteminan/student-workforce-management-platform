import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { createSemester, deactivateSemester, getSemesters, reactivateSemester, updateSemester } from './api/semestersApi'
import type { SemesterPayload } from './types'

export function useSemesters(includeInactive = false) {
  return useQuery({ queryKey: queryKeys.semesters.list({ includeInactive }), queryFn: ({ signal }) => getSemesters(includeInactive, signal) })
}

export function useSemesterMutations() {
  const queryClient = useQueryClient()
  const invalidate = async () => queryClient.invalidateQueries({ queryKey: queryKeys.semesters.all })
  return {
    create: useMutation({ mutationFn: (payload: SemesterPayload) => createSemester(payload), onSuccess: async () => { appToast.success('Semester created.'); await invalidate() } }),
    update: useMutation({ mutationFn: ({ id, payload }: { id: string; payload: SemesterPayload }) => updateSemester(id, payload), onSuccess: async () => { appToast.success('Semester updated.'); await invalidate() } }),
    deactivate: useMutation({ mutationFn: (id: string) => deactivateSemester(id), onSuccess: async () => { appToast.success('Semester deactivated.'); await invalidate() } }),
    reactivate: useMutation({ mutationFn: (id: string) => reactivateSemester(id), onSuccess: async () => { appToast.success('Semester reactivated.'); await invalidate() } }),
  }
}
