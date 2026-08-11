import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from './button'

export type PaginationState = {
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export type PaginationProps = PaginationState & {
  onPageChange: (page: number) => void
}

export function Pagination({ page, pageSize, totalCount, totalPages, hasNextPage, hasPreviousPage, onPageChange }: PaginationProps) {
  const start = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, totalCount)

  return (
    <nav aria-label="Pagination" className="flex flex-col gap-2 border-t border-border px-4 py-3 text-sm text-text-secondary sm:flex-row sm:items-center sm:justify-between">
      <span>
        Showing {start}-{end} of {totalCount}
      </span>
      <div className="flex items-center gap-2">
        <Button variant="outline" size="sm" disabled={!hasPreviousPage} onClick={() => onPageChange(page - 1)} iconBefore={<ChevronLeft aria-hidden="true" className="h-4 w-4" />}>
          Previous
        </Button>
        <span className="min-w-24 text-center text-xs text-text-muted">
          Page {page} of {Math.max(totalPages, 1)}
        </span>
        <Button variant="outline" size="sm" disabled={!hasNextPage} onClick={() => onPageChange(page + 1)} iconAfter={<ChevronRight aria-hidden="true" className="h-4 w-4" />}>
          Next
        </Button>
      </div>
    </nav>
  )
}
