import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { getDashboardAnalytics, getRequestAnalytics, getTasksByCategory, getTasksByStatus, getWorkloadDistribution } from './api/analyticsApi'

export function useAnalytics() {
  return {
    dashboard: useQuery({ queryKey: queryKeys.analytics.dashboard(), queryFn: ({ signal }) => getDashboardAnalytics(signal) }),
    taskStatus: useQuery({ queryKey: queryKeys.analytics.taskStatus(), queryFn: ({ signal }) => getTasksByStatus(signal) }),
    taskCategory: useQuery({ queryKey: queryKeys.analytics.taskCategory(), queryFn: ({ signal }) => getTasksByCategory(signal) }),
    workload: useQuery({ queryKey: queryKeys.analytics.workload(), queryFn: ({ signal }) => getWorkloadDistribution(signal) }),
    requests: useQuery({ queryKey: queryKeys.analytics.requests(), queryFn: ({ signal }) => getRequestAnalytics(signal) }),
  }
}
