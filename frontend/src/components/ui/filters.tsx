import { Filter, X } from 'lucide-react'
import { type ReactNode } from 'react'
import { Button, type ButtonProps } from './button'
import { Badge } from './badge'

export function FilterButton(props: ButtonProps) {
  return <Button variant="outline" iconBefore={<Filter aria-hidden="true" className="h-4 w-4" />} {...props} />
}

export function FilterChip({ label, onRemove }: { label: string; onRemove?: () => void }) {
  return (
    <Badge variant="neutral">
      {label}
      {onRemove ? (
        <button type="button" aria-label={`Remove ${label} filter`} className="ml-0.5 rounded-full text-text-muted hover:text-text-primary focus-visible:outline-brand" onClick={onRemove}>
          <X aria-hidden="true" className="h-3 w-3" />
        </button>
      ) : null}
    </Badge>
  )
}

export function ActiveFilters({ children, clearAction }: { children: ReactNode; clearAction?: ReactNode }) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      {children}
      {clearAction}
    </div>
  )
}
