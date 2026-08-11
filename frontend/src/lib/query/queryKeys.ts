export const queryKeys = {
  auth: {
    all: ['auth'] as const,
    currentUser: () => [...queryKeys.auth.all, 'current-user'] as const,
    sessions: (sessionId: string | null) => [...queryKeys.auth.all, 'sessions', sessionId] as const,
  },
  tasks: {
    all: ['tasks'] as const,
    list: (filters: Record<string, unknown> = {}) => [...queryKeys.tasks.all, 'list', filters] as const,
    detail: (id: string) => [...queryKeys.tasks.all, 'detail', id] as const,
  },
  students: {
    all: ['students'] as const,
    detail: (id: string) => [...queryKeys.students.all, 'detail', id] as const,
  },
} as const
