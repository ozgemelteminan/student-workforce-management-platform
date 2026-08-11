import type { PaginatedResult } from '../tasks/types'

export type MarketplaceListingStatus = 'PUBLISHED' | 'UNPUBLISHED' | 'CLOSED'
export type MarketplaceApprovalMode = 'AUTO_APPROVE' | 'MANUAL_APPROVAL'
export type MarketplaceClaimStatus = 'PENDING' | 'APPROVED' | 'REJECTED' | 'CANCELLED' | 'EXPIRED'

export type MarketplaceListing = {
  id: string
  taskId: string
  status: MarketplaceListingStatus
  approvalMode: MarketplaceApprovalMode
  publishedAt?: string
  expiresAt?: string
  concurrencyToken: string
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
