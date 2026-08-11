import { apiRequest } from '../../../lib/api'
import type { MarketplaceClaim, MarketplaceFilters, MarketplaceList, MarketplaceListing, MarketplaceApprovalMode } from '../types'

function params(filters: Record<string, string | number | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function getMarketplaceListings(filters: MarketplaceFilters, signal?: AbortSignal) {
  return apiRequest<MarketplaceList>(`/marketplace/listings${params(filters)}`, { signal })
}

export function publishTask(taskId: string, approvalMode: MarketplaceApprovalMode, expiresAt?: string) {
  return apiRequest<MarketplaceListing>(`/marketplace/tasks/${taskId}/publish`, { method: 'POST', body: { approvalMode, expiresAt } })
}

export function unpublishListing(id: string) {
  return apiRequest<MarketplaceListing>(`/marketplace/listings/${id}/unpublish`, { method: 'POST' })
}

export function claimMarketplaceListing(id: string, expiresAt?: string) {
  return apiRequest<MarketplaceClaim>(`/marketplace/listings/${id}/claims`, { method: 'POST', body: { expiresAt } })
}

export function approveMarketplaceClaim(id: string) {
  return apiRequest<MarketplaceClaim>(`/marketplace/claims/${id}/approve`, { method: 'POST' })
}

export function rejectMarketplaceClaim(id: string) {
  return apiRequest<MarketplaceClaim>(`/marketplace/claims/${id}/reject`, { method: 'POST' })
}

export function cancelMarketplaceClaim(id: string) {
  return apiRequest<MarketplaceClaim>(`/marketplace/claims/${id}/cancel`, { method: 'POST' })
}
