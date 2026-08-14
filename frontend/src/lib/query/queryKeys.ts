export const queryKeys = {
  auth: {
    all: ['auth'] as const,
    currentUser: () => [...queryKeys.auth.all, 'current-user'] as const,
    sessions: (sessionId: string | null) => [...queryKeys.auth.all, 'sessions', sessionId] as const,
  },
  tasks: {
    all: ['tasks'] as const,
    list: (filters: Record<string, unknown> = {}) => [...queryKeys.tasks.all, 'list', filters] as const,
    my: (filters: Record<string, unknown> = {}) => [...queryKeys.tasks.all, 'my', filters] as const,
    detail: (id: string) => [...queryKeys.tasks.all, 'detail', id] as const,
    checklist: (id: string) => [...queryKeys.tasks.detail(id), 'checklist'] as const,
    comments: (id: string) => [...queryKeys.tasks.detail(id), 'comments'] as const,
    dependencies: (id: string) => [...queryKeys.tasks.detail(id), 'dependencies'] as const,
    skills: (id: string) => [...queryKeys.tasks.detail(id), 'skills'] as const,
    submissions: (id: string) => [...queryKeys.tasks.detail(id), 'submissions'] as const,
    submissionVersions: (taskId: string, submissionId: string) => [...queryKeys.tasks.submissions(taskId), submissionId, 'versions'] as const,
    history: (id: string) => [...queryKeys.tasks.detail(id), 'history'] as const,
    recommendations: (id: string) => [...queryKeys.tasks.detail(id), 'recommendations'] as const,
    feedback: (id: string, filters: Record<string, unknown> = {}) => [...queryKeys.tasks.detail(id), 'feedback', filters] as const,
  },
  students: {
    all: ['students'] as const,
    list: (filters: Record<string, unknown> = {}) => [...queryKeys.students.all, 'list', filters] as const,
    detail: (id: string) => [...queryKeys.students.all, 'detail', id] as const,
    me: () => [...queryKeys.students.all, 'me'] as const,
    feedback: (id: string, filters: Record<string, unknown> = {}) => [...queryKeys.students.detail(id), 'feedback', filters] as const,
    skills: (id: string) => [...queryKeys.students.detail(id), 'skills'] as const,
  },
  semesters: {
    all: ['semesters'] as const,
    active: () => [...queryKeys.semesters.all, 'active'] as const,
  },
  schedules: {
    all: ['schedules'] as const,
    student: (studentId: string, semesterId?: string) => [...queryKeys.schedules.all, 'student', studentId, semesterId ?? 'all'] as const,
  },
  availability: {
    all: ['availability'] as const,
    student: (studentId: string, semesterId?: string) => [...queryKeys.availability.all, 'student', studentId, semesterId ?? 'all'] as const,
  },
  requests: {
    all: ['requests'] as const,
    list: (filters: Record<string, unknown> = {}) => [...queryKeys.requests.all, 'list', filters] as const,
  },
  reviews: {
    all: ['reviews'] as const,
    queue: () => [...queryKeys.reviews.all, 'queue'] as const,
    versions: (submissionId: string) => [...queryKeys.reviews.all, 'submission', submissionId, 'versions'] as const,
  },
  files: {
    all: ['files'] as const,
    list: (filters: Record<string, unknown> = {}) => [...queryKeys.files.all, 'list', filters] as const,
    folders: (parentFolderId: string | null = null) => [...queryKeys.files.all, 'folders', parentFolderId ?? 'root'] as const,
  },
  announcements: {
    all: ['announcements'] as const,
    list: (filters: Record<string, unknown> = {}) => [...queryKeys.announcements.all, 'list', filters] as const,
    detail: (id: string) => [...queryKeys.announcements.all, 'detail', id] as const,
  },
  notifications: {
    all: ['notifications'] as const,
    list: (filters: Record<string, unknown> = {}) =>
      [...queryKeys.notifications.all, 'list', filters] as const,
    unreadCount: () =>
      [...queryKeys.notifications.all, 'unread-count'] as const,
    preferences: () =>
      [...queryKeys.notifications.all, 'preferences'] as const,
  },
  categories: {
    all: ['categories'] as const,
  },
  skills: {
    all: ['skills'] as const,
  },
  marketplace: {
    all: ['marketplace'] as const,
    list: (filters: Record<string, unknown> = {}) => [...queryKeys.marketplace.all, 'list', filters] as const,
  },
  dashboard: {
    all: ['dashboard'] as const,
    attention: (scope: Record<string, unknown> = {}) => [...queryKeys.dashboard.all, 'attention', scope] as const,
  },
  templates: {
    all: ['templates'] as const,
    list: (filters: Record<string, unknown> = {}) => [...queryKeys.templates.all, 'list', filters] as const,
    detail: (id: string) => [...queryKeys.templates.all, 'detail', id] as const,
  },
  recurringTasks: {
    all: ['recurring-tasks'] as const,
    list: (filters: Record<string, unknown> = {}) => [...queryKeys.recurringTasks.all, 'list', filters] as const,
    detail: (id: string) => [...queryKeys.recurringTasks.all, 'detail', id] as const,
  },
  analytics: {
    all: ['analytics'] as const,
    dashboard: () => [...queryKeys.analytics.all, 'dashboard'] as const,
    taskStatus: () => [...queryKeys.analytics.all, 'tasks', 'status'] as const,
    taskCategory: () => [...queryKeys.analytics.all, 'tasks', 'category'] as const,
    workload: () => [...queryKeys.analytics.all, 'workload'] as const,
    requests: () => [...queryKeys.analytics.all, 'requests'] as const,
  },
  audit: {
    all: ['audit'] as const,
    list: (filters: Record<string, unknown> = {}) => [...queryKeys.audit.all, 'list', filters] as const,
    detail: (id: string) => [...queryKeys.audit.all, 'detail', id] as const,
  },
  settings: {
    all: ['settings'] as const,
  },
  exports: {
    all: ['exports'] as const,
    list: (filters: Record<string, unknown> = {}) => [...queryKeys.exports.all, 'list', filters] as const,
    detail: (id: string) => [...queryKeys.exports.all, 'detail', id] as const,
  },
} as const
