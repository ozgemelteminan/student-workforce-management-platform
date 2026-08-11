import { Construction } from 'lucide-react'
import { EmptyState } from '../components/ui/empty-state'
import { PageHeader } from '../components/ui/page-header'

export function AppPlaceholderPage({ title }: { title: string }) {
  return (
    <div className="space-y-5">
      <PageHeader title={title} description="This workspace will be implemented in a later phase." />
      <EmptyState
        title={`${title} is not implemented yet.`}
        description="Phase 2 only establishes the shared shell and design system used by later workflows."
        icon={<Construction aria-hidden="true" className="h-5 w-5" />}
      />
    </div>
  )
}
