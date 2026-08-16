import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { TaskSummary, formatTaskAssignee } from './components'
import type { Task } from './types'

const assignedStudentId = '11111111-2222-4333-8444-555555555555'

const task: Task = {
  id: 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee',
  title: 'Prepare lab inventory',
  description: 'Count and categorize equipment.',
  categoryId: 'category-1',
  priority: 'MEDIUM',
  difficulty: 'MEDIUM',
  status: 'ASSIGNED',
  createdById: 'user-1',
  assignedStudentId,
  assignedStudentName: 'Ada Lovelace',
  deadline: '2026-08-20T09:00:00Z',
  estimatedDurationMinutes: 90,
  createdAt: '2026-08-15T09:00:00Z',
  updatedAt: '2026-08-15T10:00:00Z',
  concurrencyToken: 'bbbbbbbb-cccc-4ddd-8eee-ffffffffffff',
}

describe('task presentation components', () => {
  it('renders assignees by human-readable name instead of raw student ids', () => {
    render(<TaskSummary task={task} />)

    expect(screen.getByText('Ada Lovelace')).toBeInTheDocument()
    expect(screen.queryByText(assignedStudentId)).not.toBeInTheDocument()
    expect(screen.queryByText(assignedStudentId.slice(0, 8))).not.toBeInTheDocument()
  })

  it('keeps assigned student ids internal when no display name is available', () => {
    expect(formatTaskAssignee({ assignedStudentId, assignedStudentName: null })).toBe('Name unavailable')
    expect(formatTaskAssignee({ assignedStudentId: undefined, assignedStudentName: null })).toBe('Not set')
  })
})
