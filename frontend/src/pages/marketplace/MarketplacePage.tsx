import { Store } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { Badge, Button, Card, CardContent, EmptyState, ErrorState, PageHeader, Pagination, SearchInput, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../components/ui'
import { useMarketplaceListings, useMarketplaceMutations } from '../../features/marketplace/useMarketplaceQueries'
import type { MarketplaceListingStatus } from '../../features/marketplace/types'
import { useTasks } from '../../features/tasks/useTaskQueries'
import { TaskDeadline, TaskPriorityBadge, TaskStatusBadge } from '../../features/tasks/components'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatIstanbulDate } from '../../lib/date-time'

export function MarketplacePage() {
  const { user } = useAuth()
  const student = user?.roles.includes('STUDENT') ?? false
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<MarketplaceListingStatus | undefined>('PUBLISHED')
  const filters = useMemo(() => ({ page, pageSize: 12, search: search || undefined, status }), [page, search, status])
  const listings = useMarketplaceListings(filters)
  const tasks = useTasks({ page: 1, pageSize: 100 }, true)
  const mutations = useMarketplaceMutations()
  const taskById = new Map((tasks.data?.items ?? []).map((task) => [task.id, task]))

  return (
    <div className="space-y-5">
      <PageHeader title="Marketplace" description="Internal open work pool backed by real marketplace listing records." />
      <Card>
        <CardContent className="flex flex-col gap-3 md:flex-row">
          <SearchInput label="Search marketplace listings" value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} onClear={() => setSearch('')} />
          <Select value={status ?? 'any'} onValueChange={(value) => { setStatus(value === 'any' ? undefined : value as MarketplaceListingStatus); setPage(1) }}>
            <SelectTrigger className="md:w-56" aria-label="Marketplace status"><SelectValue /></SelectTrigger>
            <SelectContent><SelectItem value="PUBLISHED">Published</SelectItem><SelectItem value="CLOSED">Closed</SelectItem><SelectItem value="UNPUBLISHED">Unpublished</SelectItem><SelectItem value="any">Any status</SelectItem></SelectContent>
          </Select>
        </CardContent>
      </Card>
      {listings.isError ? <ErrorState title="Could not load marketplace." description="Marketplace listings are unavailable right now." retryAction={<Button variant="outline" onClick={() => void listings.refetch()}>Retry</Button>} /> : null}
      {!listings.isLoading && !listings.isError && listings.data?.items.length === 0 ? <EmptyState icon={<Store className="h-5 w-5" />} title="No marketplace listings." description="No open work matches the current filters." /> : null}
      <div className="grid gap-4 lg:grid-cols-2">
        {listings.data?.items.map((listing) => {
          const task = taskById.get(listing.taskId)
          return (
            <Card key={listing.id}>
              <CardContent className="space-y-3">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <Link className="font-semibold text-text-primary hover:text-brand" to={`/tasks/${listing.taskId}`}>{task?.title ?? `Task ${listing.taskId.slice(0, 8)}`}</Link>
                    <p className="mt-1 text-sm text-text-secondary">{task?.description || 'Task summary is loaded from the accessible task list when available.'}</p>
                  </div>
                  <Badge variant={listing.status === 'PUBLISHED' ? 'success' : 'neutral'}>{listing.status}</Badge>
                </div>
                {task ? <div className="flex flex-wrap gap-2"><TaskStatusBadge status={task.status} /><TaskPriorityBadge priority={task.priority} /><TaskDeadline task={task} compact /></div> : null}
                <div className="flex items-center justify-between gap-2 text-sm text-text-secondary"><span>{listing.approvalMode}</span><span>{listing.publishedAt ? formatIstanbulDate(listing.publishedAt) : 'Not published'}</span></div>
                {student && listing.status === 'PUBLISHED' ? <Button className="w-full" isLoading={mutations.claim.isPending} onClick={() => void mutations.claim.mutate(listing.id)}>Claim task</Button> : null}
              </CardContent>
            </Card>
          )
        })}
      </div>
      {listings.data ? <Card><Pagination {...listings.data} onPageChange={setPage} /></Card> : null}
    </div>
  )
}
