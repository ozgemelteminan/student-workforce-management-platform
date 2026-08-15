import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { getSkills } from '../skills/api/skillsApi'
import { activateStudent, deactivateStudent, getCurrentStudent, getStudent, getStudentFeedback, getStudents, getStudentSkills, updateStudent, upsertStudentSkill } from './api/studentsApi'
import type { StudentFilters, UpsertStudentSkillPayload, UpdateStudentPayload } from './types'

export function useStudents(filters: StudentFilters) {
  return useQuery({ queryKey: queryKeys.students.list(stableFilters(filters)), queryFn: ({ signal }) => getStudents(filters, signal) })
}

export function useStudent(id: string | undefined) {
  return useQuery({ queryKey: queryKeys.students.detail(id ?? 'missing'), queryFn: ({ signal }) => getStudent(id ?? '', signal), enabled: Boolean(id) })
}

export function useCurrentStudent(enabled = true) {
  return useQuery({ queryKey: queryKeys.students.me(), queryFn: ({ signal }) => getCurrentStudent(signal), enabled })
}

export function useStudentFeedback(studentId: string | undefined, page = 1, pageSize = 10) {
  return useQuery({
    queryKey: queryKeys.students.feedback(studentId ?? 'missing', { page, pageSize }),
    queryFn: ({ signal }) => getStudentFeedback(studentId ?? '', page, pageSize, signal),
    enabled: Boolean(studentId),
  })
}

export function useStudentSkills(studentId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.students.skills(studentId ?? 'missing'),
    queryFn: ({ signal }) => getStudentSkills(studentId ?? '', signal),
    enabled: Boolean(studentId),
  })
}

export function useSkillCatalog() {
  return useQuery({ queryKey: queryKeys.skills.list({ includeInactive: false }), queryFn: ({ signal }) => getSkills(false, signal) })
}

export function useStudentMutations(studentId?: string) {
  const queryClient = useQueryClient()
  const invalidate = async (id = studentId) => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.students.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all }),
      id ? queryClient.invalidateQueries({ queryKey: queryKeys.students.detail(id) }) : Promise.resolve(),
    ])
  }

  return {
    update: useMutation({
      mutationFn: ({ id, payload }: { id: string; payload: UpdateStudentPayload }) => updateStudent(id, payload),
      onSuccess: async (student) => {
        appToast.success('Student profile updated.')
        await invalidate(student.id)
      },
    }),
    activate: useMutation({
      mutationFn: (id: string) => activateStudent(id),
      onSuccess: async (student) => {
        appToast.success('Student activated.')
        await invalidate(student.id)
      },
    }),
    deactivate: useMutation({
      mutationFn: (id: string) => deactivateStudent(id),
      onSuccess: async (student) => {
        appToast.success('Student deactivated.')
        await invalidate(student.id)
      },
    }),
    upsertSkill: useMutation({
      mutationFn: ({ id, payload }: { id: string; payload: UpsertStudentSkillPayload }) => upsertStudentSkill(id, payload),
      onSuccess: async (_, variables) => {
        appToast.success('Student skill saved.')
        await Promise.all([
          queryClient.invalidateQueries({ queryKey: queryKeys.students.skills(variables.id) }),
          queryClient.invalidateQueries({ queryKey: queryKeys.students.detail(variables.id) }),
        ])
      },
    }),
  }
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
