import { type HTMLAttributes } from 'react'
import { cn } from '../../lib/utils/cn'

type BadgeVariant = 'neutral' | 'brand' | 'success' | 'warning' | 'info' | 'danger'

export type BadgeProps = HTMLAttributes<HTMLSpanElement> & {
  variant?: BadgeVariant
}

const badgeClasses: Record<BadgeVariant, string> = {
  neutral: 'border-border bg-surface-secondary text-text-secondary',
  brand: 'border-brand/20 bg-brand-subtle text-brand',
  success: 'border-success/20 bg-success/10 text-success',
  warning: 'border-warning/25 bg-warning/10 text-warning',
  info: 'border-info/20 bg-info/10 text-info',
  danger: 'border-destructive/20 bg-destructive/10 text-destructive',
}

export function Badge({ className, variant = 'neutral', ...props }: BadgeProps) {
  return (
    <span
      className={cn('inline-flex h-6 items-center gap-1 rounded-full border px-2 text-xs font-medium leading-none', badgeClasses[variant], className)}
      {...props}
    />
  )
}
