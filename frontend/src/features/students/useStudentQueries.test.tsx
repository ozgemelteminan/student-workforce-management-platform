import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { queryKeys } from '../../lib/query'
import { useStudentMutations } from './useStudentQueries'
import type { PropsWithChildren } from 'react'

vi.mock('../../lib/toast', () => ({
  appToast: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  },
}))

describe('student query hooks', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('invalidates student skills and detail after skill upsert succeeds', async () => {
    const studentId = 'student-id'
    const fetchMock = vi.fn(async () => Response.json({ id: 'relationship-id', studentId, skillId: 'skill-id', level: 'ADVANCED' }))
    vi.stubGlobal('fetch', fetchMock)
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
    queryClient.setQueryData(queryKeys.students.skills(studentId), [{ skillId: 'skill-id', name: 'Data QA', level: 'BEGINNER' }])
    queryClient.setQueryData(queryKeys.students.detail(studentId), { student: { id: studentId }, skillCount: 1 })
    const wrapper = ({ children }: PropsWithChildren) => <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>

    const { result } = renderHook(() => useStudentMutations(studentId), { wrapper })
    result.current.upsertSkill.mutate({ id: studentId, payload: { skillId: 'skill-id', level: 'ADVANCED' } })

    await waitFor(() => expect(result.current.upsertSkill.isSuccess).toBe(true))
    expect(queryClient.getQueryState(queryKeys.students.skills(studentId))?.isInvalidated).toBe(true)
    expect(queryClient.getQueryState(queryKeys.students.detail(studentId))?.isInvalidated).toBe(true)
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})
