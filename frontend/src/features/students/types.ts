import type { PaginatedResult, Skill, SkillLevel, Task, Feedback } from '../tasks/types'

export type { Feedback, PaginatedResult, Skill, SkillLevel, Task }

export type Student = {
  id: string
  userId: string
  firstName: string
  lastName: string
  email: string
  department: string
  weeklyTargetMinutes?: number | null
  isActive: boolean
  createdAt: string
  updatedAt?: string
  concurrencyToken: string
}

export type StudentProfile = {
  student: Student
  activeTaskCount: number
  completedTaskCount: number
  currentWorkloadMinutes: number
  skillCount: number
  scheduleEntryCount: number
  availabilityEntryCount: number
}

export type StudentFilters = {
  page: number
  pageSize: number
  search?: string
  sortBy?: 'name' | 'email' | 'department' | 'created'
  sortDirection?: 'asc' | 'desc'
}

export type UpdateStudentPayload = {
  firstName: string
  lastName: string
  email: string
  department: string
  weeklyTargetMinutes?: number | null
}

export type UpsertStudentSkillPayload = {
  skillId: string
  level: SkillLevel
}

export type StudentSkillDetail = {
  skillId: string
  name: string
  level: SkillLevel
}
