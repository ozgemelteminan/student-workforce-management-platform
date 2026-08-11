import type { UserRole } from '../../lib/auth/authTypes'
import type { Task } from './types'

export type AttentionItem = {
  id: string
  title: string
  description: string
  href: string
  priority: number
  tone: 'danger' | 'warning' | 'info' | 'neutral'
}

export function deriveAttentionItems(tasks: Task[], roles: readonly UserRole[], now = new Date()): AttentionItem[] {
  const isStaff = roles.includes('ADMIN') || roles.includes('TASK_MANAGER')
  const canReview = roles.includes('ADMIN') || roles.includes('REVIEWER')
  return tasks
    .flatMap((task) => {
      const items: AttentionItem[] = []
      const deadline = new Date(task.deadline)
      const terminal = task.status === 'COMPLETED' || task.status === 'CANCELLED' || task.status === 'CANNOT_COMPLETE'
      if (!terminal && (task.status === 'OVERDUE' || deadline.getTime() < now.getTime())) {
        items.push({ id: `${task.id}:overdue`, title: task.title, description: 'Overdue task needs attention.', href: `/tasks/${task.id}`, priority: 10, tone: 'danger' })
      }
      if (canReview && task.status === 'SUBMITTED_FOR_REVIEW') {
        items.push({ id: `${task.id}:review`, title: task.title, description: 'Submission is waiting for review.', href: `/tasks/${task.id}`, priority: 30, tone: 'info' })
      }
      if (!terminal && deadline.getTime() >= now.getTime() && deadline.getTime() <= now.getTime() + 48 * 60 * 60 * 1000) {
        items.push({ id: `${task.id}:due-soon`, title: task.title, description: 'Deadline is approaching.', href: `/tasks/${task.id}`, priority: 40, tone: 'warning' })
      }
      if (isStaff && !task.assignedStudentId && !terminal) {
        items.push({ id: `${task.id}:unassigned`, title: task.title, description: 'No student is assigned.', href: `/tasks/${task.id}`, priority: 50, tone: 'neutral' })
      }
      return items
    })
    .sort((a, b) => a.priority - b.priority || a.title.localeCompare(b.title))
}

export function focusModeTasks(tasks: Task[], now = new Date()): Task[] {
  return tasks
    .filter((task) => task.status !== 'COMPLETED' && task.status !== 'CANCELLED' && task.status !== 'CANNOT_COMPLETE')
    .sort((a, b) => {
      const aOverdue = a.status === 'OVERDUE' || new Date(a.deadline).getTime() < now.getTime()
      const bOverdue = b.status === 'OVERDUE' || new Date(b.deadline).getTime() < now.getTime()
      if (aOverdue !== bOverdue) return aOverdue ? -1 : 1
      return new Date(a.deadline).getTime() - new Date(b.deadline).getTime()
    })
}
