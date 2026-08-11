export type TaskStatus = 'ASSIGNED' | 'ACCEPTED' | 'IN_PROGRESS' | 'SUBMITTED_FOR_REVIEW' | 'COMPLETED' | 'INCOMPLETE' | 'CANNOT_COMPLETE' | 'CANCELLED' | 'OVERDUE'
export type TaskPriority = 'LOW' | 'MEDIUM' | 'HIGH' | 'URGENT'
export type TaskDifficulty = 'EASY' | 'MEDIUM' | 'HARD'
export type TaskCommentVisibility = 'STUDENT_VISIBLE' | 'INTERNAL'
export type SkillLevel = 'BEGINNER' | 'INTERMEDIATE' | 'ADVANCED' | 'EXPERT'
export type AssignmentStatus = 'ACTIVE' | 'UNASSIGNED' | 'REASSIGNED'
export type AssignmentMode = 'MANUAL' | 'MARKETPLACE' | 'REASSIGNMENT'
export type SubmissionStatus = 'DRAFT' | 'SUBMITTED_FOR_REVIEW' | 'APPROVED' | 'REVISION_REQUESTED'
export type FileStatus = 'UPLOAD_PENDING' | 'UPLOADED' | 'CONFIRMED' | 'FAILED' | 'DELETED'

export type PaginatedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export type Task = {
  id: string
  title: string
  description?: string
  categoryId: string
  semesterId?: string
  priority: TaskPriority
  difficulty: TaskDifficulty
  status: TaskStatus
  createdById: string
  assignedStudentId?: string
  startDate?: string
  deadline: string
  estimatedDurationMinutes: number
  createdAt: string
  updatedAt: string
  completedAt?: string
  concurrencyToken: string
}

export type TaskFilters = {
  page: number
  pageSize: number
  search?: string
  sortBy?: 'deadline' | 'priority' | 'created' | 'workload'
  sortDirection?: 'asc' | 'desc'
  status?: TaskStatus
  priority?: TaskPriority
  difficulty?: TaskDifficulty
  categoryId?: string
  studentId?: string
  isAssigned?: boolean
  deadlineFrom?: string
  deadlineTo?: string
}

export type RequiredSkillPayload = {
  skillId: string
  minimumLevel: SkillLevel
}

export type TaskFormPayload = {
  title: string
  description?: string
  categoryId: string
  semesterId?: string
  priority: TaskPriority
  difficulty: TaskDifficulty
  startDate?: string
  deadline: string
  estimatedDurationMinutes: number
  requiredSkills?: RequiredSkillPayload[]
}

export type UpdateTaskPayload = TaskFormPayload & {
  concurrencyToken: string
}

export type Category = {
  id: string
  name: string
  description?: string
}

export type Student = {
  id: string
  userId: string
  firstName: string
  lastName: string
  email: string
  department: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
  concurrencyToken: string
}

export type Skill = {
  id: string
  name: string
  description?: string
}

export type TaskChecklistItem = {
  id: string
  taskId: string
  title: string
  isCompleted: boolean
  completedAt?: string
  completedById?: string
  order: number
}

export type TaskComment = {
  id: string
  taskId: string
  authorId: string
  content: string
  visibility: TaskCommentVisibility
  createdAt: string
  updatedAt: string
}

export type TaskDependency = {
  id: string
  taskId: string
  dependsOnTaskId: string
}

export type TaskRequiredSkill = {
  id: string
  taskId: string
  skillId: string
  skillName?: string
  minimumLevel: SkillLevel
}

export type TaskAssignmentHistory = {
  id: string
  taskId: string
  studentId: string
  assignedByUserId: string
  assignedAt: string
  unassignedAt?: string
  status: AssignmentStatus
  mode: AssignmentMode
  isActive: boolean
  reason?: string
}

export type AssignmentRecommendation = {
  studentId: string
  studentName: string
  score: number
  skillScore: number
  availabilityScore: number
  workloadScore: number
  previousExperienceScore: number
  activeWorkloadMinutes: number
  reasons: string[]
}

export type Submission = {
  id: string
  taskId: string
  submittedById: string
  status: SubmissionStatus
  submittedAt?: string
  concurrencyToken: string
}

export type SubmissionUploadIntent = {
  submissionVersionId: string
  taskSubmissionId: string
  versionNumber: number
  storageKey: string
  fileName: string
  fileSize: number
  mimeType: string
  fileExtension: string
  fileStatus: FileStatus
  signedUploadUrl: string
  uploadMethod: string
  requiredHeaders: Record<string, string>
  expiresAt: string
}

export type SubmissionVersion = {
  id: string
  taskSubmissionId: string
  versionNumber: number
  fileName: string
  storageKey: string
  fileSize: number
  mimeType: string
  fileExtension: string
  contentHash?: string
  fileStatus: FileStatus
  uploadedById: string
  uploadedAt: string
  confirmedAt?: string
}

export type Feedback = {
  id: string
  taskId: string
  studentId: string
  createdById: string
  rating?: number
  comment?: string
  createdAt: string
}

export type SubmissionDownloadUrl = {
  submissionVersionId: string
  fileName: string
  fileSize: number
  signedDownloadUrl: string
  expiresAt: string
}
