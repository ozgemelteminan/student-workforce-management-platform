import { type ReactNode } from 'react'
import { cn } from '../../lib/utils/cn'

export type EmptyStateProps = {
  title: string
  description?: string
  icon?: ReactNode
  primaryAction?: ReactNode
  secondaryAction?: ReactNode
  className?: string
}

export function EmptyState({ title, description, icon, primaryAction, secondaryAction, className }: EmptyStateProps) {
  return (
    <section className={cn('flex min-h-48 flex-col items-center justify-center rounded-lg border border-dashed border-border bg-surface px-6 py-8 text-center', className)}>
      {icon ? <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-surface-secondary text-text-secondary">{icon}</div> : null}
      <h2 className="text-sm font-semibold text-text-primary">{title}</h2>
      {description ? <p className="mt-1 max-w-md text-sm text-text-secondary">{description}</p> : null}
      {primaryAction || secondaryAction ? <div className="mt-4 flex flex-wrap justify-center gap-2">{secondaryAction}{primaryAction}</div> : null}
    </section>
  )
}
