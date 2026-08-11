import type { PaginatedResult, SkillLevel, TaskPriority } from '../tasks/types'

export type MarketplaceListingStatus = 'DRAFT' | 'PUBLISHED' | 'UNPUBLISHED' | 'EXPIRED' | 'CLOSED'
export type MarketplaceApprovalMode = 'AUTO_APPROVAL' | 'MANUAL_APPROVAL'
export type MarketplaceClaimStatus = 'PENDING' | 'APPROVED' | 'REJECTED' | 'CANCELLED' | 'EXPIRED'

export type MarketplaceRequiredSkillSummary = {
  skillId: string
  skillName: string
  minimumLevel: SkillLevel
}

export type MarketplaceTaskSummary = {
  taskId: string
  title: string
  description?: string
  categoryId: string
  categoryName?: string
  priority: TaskPriority
  deadline: string
  estimatedDurationMinutes: number
  requiredSkills: MarketplaceRequiredSkillSummary[]
}

export type MarketplaceListing = {
  id: string
  taskId: string
  status: MarketplaceListingStatus
  approvalMode: MarketplaceApprovalMode
  publishedAt?: string
  expiresAt?: string
  concurrencyToken: string
  taskSummary?: MarketplaceTaskSummary
}

export type MarketplaceClaim = {
  id: string
  marketplaceListingId: string
  studentId: string
  status: MarketplaceClaimStatus
  claimedAt: string
  expiresAt?: string
  approvedAt?: string
  rejectedAt?: string
  concurrencyToken: string
}

export type MarketplaceFilters = {
  page: number
  pageSize: number
  search?: string
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
  status?: MarketplaceListingStatus
}

export type MarketplaceList = PaginatedResult<MarketplaceListing>
