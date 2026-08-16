import { differenceInCalendarDays } from 'date-fns'
import { toZonedTime } from 'date-fns-tz'
import { DISPLAY_TIME_ZONE, formatIstanbulDate, formatIstanbulDateTime } from '../../lib/date-time'
import type { AssignmentMode, AssignmentStatus, FileStatus, SubmissionStatus, Task, TaskPriority, TaskStatus } from './types'

export const statusLabels: Record<TaskStatus, string> = {
  ASSIGNED: 'Assigned',
  ACCEPTED: 'Accepted',
  IN_PROGRESS: 'In progress',
  SUBMITTED_FOR_REVIEW: 'Submitted for review',
  COMPLETED: 'Completed',
  INCOMPLETE: 'Incomplete',
  CANNOT_COMPLETE: 'Cannot complete',
  CANCELLED: 'Cancelled',
  OVERDUE: 'Overdue',
}

export const submissionStatusLabels: Record<SubmissionStatus, string> = {
  DRAFT: 'Draft',
  SUBMITTED_FOR_REVIEW: 'Submitted for review',
  APPROVED: 'Approved',
  REVISION_REQUESTED: 'Revision requested',
}

export const fileStatusLabels: Record<FileStatus, string> = {
  UPLOAD_PENDING: 'Upload pending',
  UPLOADED: 'Uploaded',
  CONFIRMED: 'Confirmed',
  FAILED: 'Failed',
  DELETED: 'Deleted',
}

export const assignmentStatusLabels: Record<AssignmentStatus, string> = {
  ACTIVE: 'Active',
  UNASSIGNED: 'Unassigned',
  REASSIGNED: 'Reassigned',
}

export const assignmentModeLabels: Record<AssignmentMode, string> = {
  MANUAL: 'Manual',
  MARKETPLACE: 'Task pool',
  REASSIGNMENT: 'Reassignment',
}

export const priorityLabels: Record<TaskPriority, string> = {
  LOW: 'Low',
  MEDIUM: 'Medium',
  HIGH: 'High',
  URGENT: 'Urgent',
}

export function statusVariant(status: TaskStatus) {
  if (status === 'COMPLETED') return 'success' as const
  if (status === 'OVERDUE' || status === 'CANCELLED' || status === 'CANNOT_COMPLETE') return 'danger' as const
  if (status === 'INCOMPLETE') return 'warning' as const
  if (status === 'SUBMITTED_FOR_REVIEW' || status === 'IN_PROGRESS') return 'info' as const
  return 'neutral' as const
}

export function submissionStatusVariant(status: SubmissionStatus) {
  if (status === 'APPROVED') return 'success' as const
  if (status === 'REVISION_REQUESTED') return 'warning' as const
  if (status === 'SUBMITTED_FOR_REVIEW') return 'info' as const
  return 'neutral' as const
}

export function fileStatusVariant(status: FileStatus) {
  if (status === 'CONFIRMED') return 'success' as const
  if (status === 'FAILED' || status === 'DELETED') return 'danger' as const
  if (status === 'UPLOAD_PENDING' || status === 'UPLOADED') return 'warning' as const
  return 'neutral' as const
}

export function priorityVariant(priority: TaskPriority) {
  if (priority === 'URGENT') return 'danger' as const
  if (priority === 'HIGH') return 'warning' as const
  if (priority === 'MEDIUM') return 'info' as const
  return 'neutral' as const
}

export function formatDuration(minutes: number): string {
  if (minutes < 60) return `${minutes} min`
  const hours = Math.floor(minutes / 60)
  const remaining = minutes % 60
  return remaining ? `${hours} hr ${remaining} min` : `${hours} hr`
}

export function deadlineLabel(task: Pick<Task, 'deadline' | 'status'>, now = new Date()): { label: string; tone: 'danger' | 'warning' | 'neutral'; absolute: string } {
  const deadline = new Date(task.deadline)
  const zonedDeadline = toZonedTime(deadline, DISPLAY_TIME_ZONE)
  const zonedNow = toZonedTime(now, DISPLAY_TIME_ZONE)
  const days = differenceInCalendarDays(zonedDeadline, zonedNow)
  const isDone = task.status === 'COMPLETED' || task.status === 'CANCELLED' || task.status === 'CANNOT_COMPLETE'
  if (!isDone && (task.status === 'OVERDUE' || deadline.getTime() < now.getTime())) {
    return { label: days === 0 ? 'Overdue today' : `Overdue by ${Math.abs(days)} day${Math.abs(days) === 1 ? '' : 's'}`, tone: 'danger', absolute: formatIstanbulDateTime(task.deadline) }
  }
  if (days === 0) return { label: 'Due today', tone: 'warning', absolute: formatIstanbulDateTime(task.deadline) }
  if (days === 1) return { label: 'Due tomorrow', tone: 'warning', absolute: formatIstanbulDateTime(task.deadline) }
  if (days > 1 && days <= 7) return { label: `Due in ${days} days`, tone: 'warning', absolute: formatIstanbulDateTime(task.deadline) }
  return { label: formatIstanbulDate(task.deadline), tone: 'neutral', absolute: formatIstanbulDateTime(task.deadline) }
}

export function isTerminalTask(status: TaskStatus) {
  return status === 'COMPLETED' || status === 'CANCELLED' || status === 'CANNOT_COMPLETE'
}
