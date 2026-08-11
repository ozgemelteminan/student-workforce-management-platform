import { AlertCircle } from 'lucide-react'
import { type ReactNode } from 'react'
import { cn } from '../../lib/utils/cn'

export type ErrorStateVariant = 'generic' | 'not-found' | 'permission' | 'network'

export type ErrorStateProps = {
  title: string
  description: string
  variant?: ErrorStateVariant
  retryAction?: ReactNode
  navigationAction?: ReactNode
  className?: string
}

export function ErrorState({ title, description, retryAction, navigationAction, className }: ErrorStateProps) {
  return (
    <section role="alert" className={cn('rounded-lg border border-border bg-surface px-6 py-8 text-center', className)}>
      <div className="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-destructive/10 text-destructive">
        <AlertCircle aria-hidden="true" className="h-5 w-5" />
      </div>
      <h2 className="text-sm font-semibold text-text-primary">{title}</h2>
      <p className="mx-auto mt-1 max-w-md text-sm text-text-secondary">{description}</p>
      {retryAction || navigationAction ? <div className="mt-4 flex flex-wrap justify-center gap-2">{navigationAction}{retryAction}</div> : null}
    </section>
  )
}
