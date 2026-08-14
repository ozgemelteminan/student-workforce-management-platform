import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { formatWeeklyTarget, StudentSkillsList } from './StudentDetailPage'

describe('StudentSkillsList', () => {
  it('renders real skill names and levels', () => {
    render(<StudentSkillsList skills={[{ skillId: 'skill-id', name: 'Data QA', level: 'INTERMEDIATE' }]} isLoading={false} isError={false} onRetry={vi.fn()} />)

    expect(screen.getByText('Data QA')).toBeInTheDocument()
    expect(screen.getByText('Intermediate')).toBeInTheDocument()
  })

  it('renders an empty state when no skills exist', () => {
    render(<StudentSkillsList skills={[]} isLoading={false} isError={false} onRetry={vi.fn()} />)

    expect(screen.getByText('No skills added yet.')).toBeInTheDocument()
  })

  it('keeps skill loading and errors scoped to the skills subsection', () => {
    const retry = vi.fn()
    const { rerender } = render(<StudentSkillsList skills={[]} isLoading isError={false} onRetry={retry} />)

    expect(screen.getByLabelText('Loading student skills')).toBeInTheDocument()
    rerender(<StudentSkillsList skills={[]} isLoading={false} isError onRetry={retry} />)
    expect(screen.getByText('Could not load skills.')).toBeInTheDocument()
  })
})

describe('formatWeeklyTarget', () => {
  it('shows missing student weekly target as not configured', () => {
    expect(formatWeeklyTarget(null)).toBe('Not configured')
    expect(formatWeeklyTarget(undefined)).toBe('Not configured')
    expect(formatWeeklyTarget(600)).toBe('10 hr')
  })
})
