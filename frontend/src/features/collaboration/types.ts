import type { PaginatedResult, TaskDifficulty, TaskPriority } from '../tasks/types'

export type { PaginatedResult }

export type TimesheetStatus = 'DRAFT' | 'SUBMITTED' | 'APPROVED' | 'NEEDS_CORRECTION'
export type MeetingType = 'IN_PERSON' | 'ONLINE'
export type MeetingStatus = 'DRAFT' | 'AVAILABILITY_REQUESTED' | 'CONFIRMED' | 'CANCELLED'
export type CampusPresence = 'ON_CAMPUS' | 'OFF_CAMPUS' | 'UNSURE'

export type TimesheetEntry = {
  id: string
  timesheetWeekId: string
  taskId: string
  workDate: string
  minutes: number
  note?: string
  concurrencyToken: string
}

export type TimesheetWeek = {
  id: string
  studentId: string
  weekStartDate: string
  weekEndDate: string
  targetMinutes: number
  status: TimesheetStatus
  totalMinutes: number
  submittedAt?: string
  reviewedAt?: string
  reviewedByUserId?: string
  reviewerComment?: string
  concurrencyToken: string
  entries: TimesheetEntry[]
}

export type TemporaryUnavailability = {
  id: string
  studentId: string
  startAt: string
  endAt: string
  category: string
  note?: string
  concurrencyToken: string
}

export type MeetingParticipant = {
  id: string
  meetingId: string
  studentId: string
  campusPresence?: CampusPresence
  availableRangesJson?: string
  note?: string
  respondedAt?: string
  concurrencyToken: string
}

export type MeetingActionItem = {
  id: string
  meetingId: string
  title: string
  assignedStudentId?: string
  taskId?: string
  isCompleted: boolean
  concurrencyToken: string
}

export type Meeting = {
  id: string
  title: string
  type: MeetingType
  status: MeetingStatus
  createdByUserId: string
  responseDeadline: string
  confirmedStartAt?: string
  confirmedEndAt?: string
  location?: string
  agenda?: string
  notes?: string
  concurrencyToken: string
  participants: MeetingParticipant[]
  actionItems: MeetingActionItem[]
}

export type MeetingSlotRecommendation = {
  startAt: string
  endAt: string
  availableCount: number
  participantCount: number
  onCampusCount: number
}

export type CreateMeetingPayload = {
  title: string
  type: MeetingType
  responseDeadline: string
  participantStudentIds: string[]
  location?: string
  agenda?: string
}

export type ActionItemTaskPayload = {
  categoryId: string
  semesterId?: string
  priority: TaskPriority
  difficulty: TaskDifficulty
  deadline: string
  estimatedDurationMinutes: number
}
