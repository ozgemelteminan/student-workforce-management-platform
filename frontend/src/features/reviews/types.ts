import type { Submission, SubmissionVersion, Task } from '../tasks/types'

export type { Submission, SubmissionVersion, Task }

export type TaskReview = {
  id: string
  taskId: string
  submissionId: string
  reviewedById: string
  isApproved: boolean
  reviewerComment?: string
  createdAt: string
}

export type ReviewQueueItem = {
  task: Task
  submissions: Submission[]
}
