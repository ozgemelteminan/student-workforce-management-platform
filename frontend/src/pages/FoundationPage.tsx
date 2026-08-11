import { LayoutDashboard } from 'lucide-react'
import { EmptyState } from '../components/ui/empty-state'
import { PageHeader } from '../components/ui/page-header'
import { formatIstanbulDateTime } from '../lib/date-time'

export function FoundationPage() {
  return (
    <div className="space-y-5">
      <PageHeader title="Dashboard" description="This workspace will be implemented in a later phase." />
      <EmptyState
        title="Dashboard is not implemented yet."
        description={`Phase 2 establishes the shared shell and design system. Display timezone: Europe/Istanbul · ${formatIstanbulDateTime(new Date())}`}
        icon={<LayoutDashboard aria-hidden="true" className="h-5 w-5" />}
      />
    </div>
  )
}
