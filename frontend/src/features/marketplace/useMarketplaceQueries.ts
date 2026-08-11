import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { claimMarketplaceListing, getMarketplaceListings, publishTask, unpublishListing } from './api/marketplaceApi'
import type { MarketplaceApprovalMode, MarketplaceFilters } from './types'

export function useMarketplaceListings(filters: MarketplaceFilters) {
  return useQuery({ queryKey: queryKeys.marketplace.list(stableFilters(filters)), queryFn: ({ signal }) => getMarketplaceListings(filters, signal) })
}

export function useMarketplaceMutations() {
  const queryClient = useQueryClient()
  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.marketplace.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all }),
    ])
  }
  return {
    claim: useMutation({
      mutationFn: (listingId: string) => claimMarketplaceListing(listingId),
      onSuccess: async () => {
        appToast.success('Marketplace claim submitted.')
        await invalidate()
      },
    }),
    publish: useMutation({ mutationFn: ({ taskId, approvalMode, expiresAt }: { taskId: string; approvalMode: MarketplaceApprovalMode; expiresAt?: string }) => publishTask(taskId, approvalMode, expiresAt), onSuccess: invalidate }),
    unpublish: useMutation({ mutationFn: (listingId: string) => unpublishListing(listingId), onSuccess: invalidate }),
  }
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
