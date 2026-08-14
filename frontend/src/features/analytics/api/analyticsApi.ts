import { apiRequest } from '../../../lib/api'
import type { DashboardAnalytics, RequestAnalytics, TasksByCategory, TasksByStatus, WorkloadDistribution } from '../types'

export function getDashboardAnalytics(signal?: AbortSignal) {
  return apiRequest<DashboardAnalytics>('/analytics/dashboard', { signal })
}

export function getTasksByStatus(signal?: AbortSignal) {
  return apiRequest<TasksByStatus[]>('/analytics/tasks/status', { signal })
}

export function getTasksByCategory(signal?: AbortSignal) {
  return apiRequest<TasksByCategory[]>('/analytics/tasks/category', { signal })
}

export function getWorkloadDistribution(signal?: AbortSignal) {
  return apiRequest<WorkloadDistribution[]>('/analytics/workload', { signal })
}

export function getRequestAnalytics(signal?: AbortSignal) {
  return apiRequest<RequestAnalytics[]>('/analytics/requests', { signal })
}
