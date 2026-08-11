import { type ReactNode } from 'react'
import { cn } from '../../lib/utils/cn'
import { Breadcrumb, type BreadcrumbItem } from './breadcrumb'

export type PageHeaderProps = {
  title: string
  description?: string
  breadcrumbs?: BreadcrumbItem[]
  primaryAction?: ReactNode
  secondaryActions?: ReactNode
  metadata?: ReactNode
  tabs?: ReactNode
  className?: string
}

export function PageHeader({ title, description, breadcrumbs, primaryAction, secondaryActions, metadata, tabs, className }: PageHeaderProps) {
  return (
    <header className={cn('space-y-4', className)}>
      {breadcrumbs?.length ? <Breadcrumb items={breadcrumbs} /> : null}
      <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
        <div className="min-w-0">
          <h1 className="truncate text-xl font-semibold tracking-normal text-text-primary">{title}</h1>
          {description ? <p className="mt-1 max-w-3xl text-sm text-text-secondary">{description}</p> : null}
          {metadata ? <div className="mt-2 flex flex-wrap gap-2">{metadata}</div> : null}
        </div>
        {primaryAction || secondaryActions ? <div className="flex shrink-0 flex-wrap gap-2">{secondaryActions}{primaryAction}</div> : null}
      </div>
      {tabs}
    </header>
  )
}
