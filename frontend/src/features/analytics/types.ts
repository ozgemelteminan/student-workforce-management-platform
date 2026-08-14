import type { RequestStatus, RequestType } from '../requests/types'
import type { TaskStatus } from '../tasks/types'

export type DashboardAnalytics = {
  totalTasks: number
  activeTasks: number
  completedTasks: number
  overdueTasks: number
  pendingReviews: number
  pendingRequests: number
}

export type TasksByStatus = {
  status: TaskStatus
  count: number
}

export type TasksByCategory = {
  categoryId: string
  categoryName: string
  count: number
}

export type WorkloadDistribution = {
  studentId: string
  studentName: string
  activeWorkloadMinutes: number
  activeTaskCount: number
}

export type RequestAnalytics = {
  type: RequestType
  status: RequestStatus
  count: number
}
