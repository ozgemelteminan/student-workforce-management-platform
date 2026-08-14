import { Store } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { Badge, Button, Card, CardContent, EmptyState, ErrorState, PageHeader, Pagination, SearchInput, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../components/ui'
import { useMarketplaceListings, useMarketplaceMutations } from '../../features/marketplace/useMarketplaceQueries'
import type { MarketplaceListingStatus } from '../../features/marketplace/types'
import { TaskPriorityBadge } from '../../features/tasks/components'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatIstanbulDate } from '../../lib/date-time'
import { formatDuration } from '../../features/tasks/taskPresentation'

export function MarketplacePage() {
  const { user } = useAuth()
  const student = user?.roles.includes('STUDENT') ?? false
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<MarketplaceListingStatus | undefined>('PUBLISHED')
  const filters = useMemo(() => ({ page, pageSize: 12, search: search || undefined, status }), [page, search, status])
  const listings = useMarketplaceListings(filters)
  const mutations = useMarketplaceMutations()

  return (
    <div className="space-y-5">
      <PageHeader title="Task Pool" description="Browse and claim tasks currently open for assignment." />
      <Card>
        <CardContent className="flex flex-col gap-3 md:flex-row">
          <SearchInput label="Search task pool listings" value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} onClear={() => setSearch('')} />
          <Select value={status ?? 'any'} onValueChange={(value) => { setStatus(value === 'any' ? undefined : value as MarketplaceListingStatus); setPage(1) }}>
            <SelectTrigger className="md:w-56" aria-label="Task pool status"><SelectValue /></SelectTrigger>
            <SelectContent><SelectItem value="PUBLISHED">Published</SelectItem><SelectItem value="CLOSED">Closed</SelectItem><SelectItem value="UNPUBLISHED">Unpublished</SelectItem><SelectItem value="any">Any status</SelectItem></SelectContent>
          </Select>
        </CardContent>
      </Card>
      {listings.isError ? <ErrorState title="Could not load task pool." description="Task pool listings are unavailable right now." retryAction={<Button variant="outline" onClick={() => void listings.refetch()}>Retry</Button>} /> : null}
      {!listings.isLoading && !listings.isError && listings.data?.items.length === 0 ? <EmptyState icon={<Store className="h-5 w-5" />} title="No task pool listings." description="No open work matches the current filters." /> : null}
      <div className="grid gap-4 lg:grid-cols-2">
        {listings.data?.items.map((listing) => {
          const task = listing.taskSummary
          return (
            <Card key={listing.id}>
              <CardContent className="space-y-3">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <Link className="font-semibold text-text-primary hover:text-brand" to={`/tasks/${listing.taskId}`}>{task?.title ?? `Task ${listing.taskId.slice(0, 8)}`}</Link>
                    <p className="mt-1 text-sm text-text-secondary">{task?.description || task?.categoryName || 'Task pool summary unavailable.'}</p>
                  </div>
                  <Badge variant={listing.status === 'PUBLISHED' ? 'success' : 'neutral'}>{listing.status}</Badge>
                </div>
                {task ? <div className="flex flex-wrap items-center gap-2"><TaskPriorityBadge priority={task.priority} /><Badge variant="neutral">{formatIstanbulDate(task.deadline)}</Badge><Badge variant="neutral">{formatDuration(task.estimatedDurationMinutes)}</Badge></div> : null}
                {task?.requiredSkills.length ? <div className="flex flex-wrap gap-2">{task.requiredSkills.map((skill) => <Badge key={skill.skillId} variant="neutral">{skill.skillName} · {skill.minimumLevel}</Badge>)}</div> : null}
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
