import { addDays, startOfDay } from 'date-fns'
import { fromZonedTime } from 'date-fns-tz'
import { DISPLAY_TIME_ZONE } from '../../lib/date-time'
import type { TaskFilters, TaskPriority, TaskStatus } from './types'

const statuses: TaskStatus[] = ['ASSIGNED', 'ACCEPTED', 'IN_PROGRESS', 'SUBMITTED_FOR_REVIEW', 'COMPLETED', 'INCOMPLETE', 'CANNOT_COMPLETE', 'CANCELLED', 'OVERDUE']
const priorities: TaskPriority[] = ['LOW', 'MEDIUM', 'HIGH', 'URGENT']
const sortBy = ['deadline', 'priority', 'created', 'workload'] as const
const sortDirection = ['asc', 'desc'] as const

export type TaskView = 'all' | 'my' | 'overdue' | 'due-week' | 'needs-review'

export function filtersFromSearchParams(params: URLSearchParams): TaskFilters & { view: TaskView } {
  const page = parsePositiveInt(params.get('page'), 1)
  const pageSize = parsePositiveInt(params.get('pageSize'), 20)
  const view = parseView(params.get('view'))
  const filters: TaskFilters & { view: TaskView } = {
    view,
    page,
    pageSize,
    search: clean(params.get('search')),
    sortBy: parseEnum(params.get('sortBy'), sortBy),
    sortDirection: parseEnum(params.get('sortDirection'), sortDirection),
    status: parseEnum(params.get('status'), statuses),
    priority: parseEnum(params.get('priority'), priorities),
    categoryId: clean(params.get('categoryId')),
  }
  return applyTaskView(filters)
}

export function filtersToSearchParams(filters: TaskFilters & { view: TaskView }) {
  const params = new URLSearchParams()
  set(params, 'view', filters.view === 'all' ? undefined : filters.view)
  set(params, 'page', filters.page === 1 ? undefined : filters.page)
  set(params, 'pageSize', filters.pageSize === 20 ? undefined : filters.pageSize)
  set(params, 'search', filters.search)
  set(params, 'sortBy', filters.sortBy)
  set(params, 'sortDirection', filters.sortDirection)
  set(params, 'status', filters.status)
  set(params, 'priority', filters.priority)
  set(params, 'categoryId', filters.categoryId)
  return params
}

export function applyTaskView(filters: TaskFilters & { view: TaskView }): TaskFilters & { view: TaskView } {
  const next = { ...filters }
  if (next.view === 'overdue') next.status = 'OVERDUE'
  if (next.view === 'needs-review') next.status = 'SUBMITTED_FOR_REVIEW'
  if (next.view === 'due-week') {
    const start = fromZonedTime(startOfDay(new Date()), DISPLAY_TIME_ZONE)
    const end = fromZonedTime(addDays(startOfDay(new Date()), 7), DISPLAY_TIME_ZONE)
    next.deadlineFrom = start.toISOString()
    next.deadlineTo = end.toISOString()
  }
  return next
}

function parsePositiveInt(value: string | null, fallback: number) {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback
}

function parseEnum<const T extends readonly string[]>(value: string | null, allowed: T): T[number] | undefined {
  return value && allowed.includes(value) ? value : undefined
}

function parseView(value: string | null): TaskView {
  return value === 'my' || value === 'overdue' || value === 'due-week' || value === 'needs-review' ? value : 'all'
}

function clean(value: string | null) {
  return value?.trim() || undefined
}

function set(params: URLSearchParams, key: string, value: string | number | undefined) {
  if (value !== undefined && value !== '') params.set(key, String(value))
}
