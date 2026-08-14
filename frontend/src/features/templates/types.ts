import type { PaginatedResult, Task, TaskDifficulty, TaskPriority } from '../tasks/types'

export type { PaginatedResult, TaskDifficulty, TaskPriority }

export type TaskTemplate = {
  id: string
  title: string
  description?: string
  categoryId: string
  defaultPriority: TaskPriority
  defaultDifficulty: TaskDifficulty
  estimatedDurationMinutes: number
  createdById: string
  checklistTemplateJson?: string
  requiredSkillsTemplateJson?: string
  createdAt: string
  updatedAt?: string
}

export type TemplateFilters = {
  page: number
  pageSize: number
  search?: string
  categoryId?: string
}

export type TemplatePayload = {
  title: string
  description?: string
  categoryId: string
  defaultPriority: TaskPriority
  defaultDifficulty: TaskDifficulty
  estimatedDurationMinutes: number
  checklistTemplateJson?: string
  requiredSkillsTemplateJson?: string
}

export type CreateTaskFromTemplatePayload = {
  startDate?: string
  deadline: string
  semesterId?: string
}

export type CreatedTemplateTask = Task
