import { describe, expect, it } from 'vitest'
import { deriveAttentionItems, focusModeTasks } from './attention'
import type { Task } from './types'

const baseTask: Task = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Base task',
  categoryId: '22222222-2222-2222-2222-222222222222',
  priority: 'MEDIUM',
  difficulty: 'EASY',
  status: 'IN_PROGRESS',
  createdById: '33333333-3333-3333-3333-333333333333',
  assignedStudentId: '44444444-4444-4444-4444-444444444444',
  deadline: '2026-08-20T09:00:00Z',
  estimatedDurationMinutes: 60,
  createdAt: '2026-08-01T09:00:00Z',
  updatedAt: '2026-08-01T09:00:00Z',
  concurrencyToken: '55555555-5555-5555-5555-555555555555',
}

function task(patch: Partial<Task>): Task {
  return { ...baseTask, id: crypto.randomUUID(), ...patch }
}

describe('deriveAttentionItems', () => {
  it('prioritizes overdue, review, due-soon, then unassigned items deterministically', () => {
    const now = new Date('2026-08-11T09:00:00Z')
    const items = deriveAttentionItems([
      task({ title: 'Unassigned', assignedStudentId: undefined, deadline: '2026-08-20T09:00:00Z' }),
      task({ title: 'Review', status: 'SUBMITTED_FOR_REVIEW', deadline: '2026-08-18T09:00:00Z' }),
      task({ title: 'Overdue', status: 'IN_PROGRESS', deadline: '2026-08-10T09:00:00Z' }),
      task({ title: 'Due soon', status: 'IN_PROGRESS', deadline: '2026-08-12T09:00:00Z' }),
      task({ title: 'Complete', status: 'COMPLETED', deadline: '2026-08-10T09:00:00Z' }),
    ], ['ADMIN'], now)

    expect(items.map((item) => item.description)).toEqual([
      'Overdue task needs attention.',
      'Submission is waiting for review.',
      'Deadline is approaching.',
      'No student is assigned.',
    ])
  })

  it('does not expose review or unassigned staff attention to students', () => {
    const now = new Date('2026-08-11T09:00:00Z')
    const items = deriveAttentionItems([
      task({ status: 'SUBMITTED_FOR_REVIEW' }),
      task({ assignedStudentId: undefined }),
    ], ['STUDENT'], now)

    expect(items).toHaveLength(0)
  })
})

describe('focusModeTasks', () => {
  it('excludes terminal tasks and orders overdue before future work', () => {
    const now = new Date('2026-08-11T09:00:00Z')
    const ordered = focusModeTasks([
      task({ title: 'Done', status: 'COMPLETED' }),
      task({ title: 'Blocked', status: 'CANNOT_COMPLETE' }),
      task({ title: 'Future', status: 'IN_PROGRESS', deadline: '2026-08-14T09:00:00Z' }),
      task({ title: 'Soon', status: 'ACCEPTED', deadline: '2026-08-13T09:00:00Z' }),
      task({ title: 'Overdue', status: 'IN_PROGRESS', deadline: '2026-08-10T09:00:00Z' }),
    ], now)

    expect(ordered.map((item) => item.title)).toEqual(['Overdue', 'Soon', 'Future'])
  })
})
