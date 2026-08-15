import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { createSkill, deactivateSkill, getSkills, reactivateSkill, updateSkill } from './api/skillsApi'
import type { ReferenceDataPayload } from './types'

export function useSkills(includeInactive = false) {
  return useQuery({ queryKey: queryKeys.skills.list({ includeInactive }), queryFn: ({ signal }) => getSkills(includeInactive, signal) })
}

export function useSkillMutations() {
  const queryClient = useQueryClient()
  const invalidate = async () => queryClient.invalidateQueries({ queryKey: queryKeys.skills.all })
  return {
    create: useMutation({ mutationFn: (payload: ReferenceDataPayload) => createSkill(payload), onSuccess: async () => { appToast.success('Skill created.'); await invalidate() } }),
    update: useMutation({ mutationFn: ({ id, payload }: { id: string; payload: ReferenceDataPayload }) => updateSkill(id, payload), onSuccess: async () => { appToast.success('Skill updated.'); await invalidate() } }),
    deactivate: useMutation({ mutationFn: (id: string) => deactivateSkill(id), onSuccess: async () => { appToast.success('Skill deactivated.'); await invalidate() } }),
    reactivate: useMutation({ mutationFn: (id: string) => reactivateSkill(id), onSuccess: async () => { appToast.success('Skill reactivated.'); await invalidate() } }),
  }
}
