import { ArrowUpDown } from 'lucide-react'
import { type ReactNode } from 'react'
import { cn } from '../../lib/utils/cn'
import { EmptyState } from './empty-state'
import { SkeletonTable } from './skeleton'

export type DataTableColumn<T> = {
  key: string
  header: ReactNode
  cell: (row: T) => ReactNode
  className?: string
  sortable?: boolean
  sortLabel?: string
}

export type DataTableProps<T> = {
  columns: DataTableColumn<T>[]
  rows: T[]
  getRowKey: (row: T) => string
  isLoading?: boolean
  emptyState?: ReactNode
  onRowClick?: (row: T) => void
  rowActions?: (row: T) => ReactNode
  pagination?: ReactNode
}

export function DataTable<T>({ columns, rows, getRowKey, isLoading, emptyState, onRowClick, rowActions, pagination }: DataTableProps<T>) {
  if (isLoading) {
    return <SkeletonTable rows={5} columns={columns.length + (rowActions ? 1 : 0)} />
  }

  if (rows.length === 0) {
    return <>{emptyState ?? <EmptyState title="No records found." description="Adjust filters or check back later." />}</>
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border bg-surface">
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-border text-left text-sm">
          <thead className="bg-surface-secondary text-xs uppercase tracking-wide text-text-muted">
            <tr>
              {columns.map((column) => (
                <th key={column.key} scope="col" className={cn('whitespace-nowrap px-4 py-3 font-semibold', column.className)}>
                  <span className="inline-flex items-center gap-1.5">
                    {column.header}
                    {column.sortable ? <ArrowUpDown aria-label={column.sortLabel ?? 'Sortable column'} className="h-3.5 w-3.5" /> : null}
                  </span>
                </th>
              ))}
              {rowActions ? <th scope="col" className="w-12 px-4 py-3"><span className="sr-only">Actions</span></th> : null}
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {rows.map((row) => (
              <tr key={getRowKey(row)} className={cn('transition-colors hover:bg-surface-secondary/70', onRowClick && 'cursor-pointer')} onClick={() => onRowClick?.(row)}>
                {columns.map((column) => <td key={column.key} className={cn('px-4 py-3 align-middle text-text-primary', column.className)}>{column.cell(row)}</td>)}
                {rowActions ? <td className="px-4 py-3" onClick={(event) => event.stopPropagation()}>{rowActions(row)}</td> : null}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {pagination}
    </div>
  )
}
