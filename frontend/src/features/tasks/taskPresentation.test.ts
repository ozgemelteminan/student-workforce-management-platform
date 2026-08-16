import { describe, expect, it } from 'vitest'
import {
  assignmentModeLabels,
  assignmentStatusLabels,
  fileStatusLabels,
  submissionStatusLabels,
  statusLabels,
} from './taskPresentation'

describe('task status presentation', () => {
  it('formats user-facing enum statuses without raw enum text', () => {
    expect(statusLabels.IN_PROGRESS).toBe('In progress')
    expect(statusLabels.SUBMITTED_FOR_REVIEW).toBe('Submitted for review')
    expect(submissionStatusLabels.REVISION_REQUESTED).toBe('Revision requested')
    expect(fileStatusLabels.UPLOAD_PENDING).toBe('Upload pending')
    expect(assignmentModeLabels.MARKETPLACE).toBe('Task pool')
    expect(assignmentStatusLabels.REASSIGNED).toBe('Reassigned')

    const labels = [
      ...Object.values(statusLabels),
      ...Object.values(submissionStatusLabels),
      ...Object.values(fileStatusLabels),
      ...Object.values(assignmentModeLabels),
      ...Object.values(assignmentStatusLabels),
    ]

    expect(labels.every((label) => !label.includes('_'))).toBe(true)
  })
})
