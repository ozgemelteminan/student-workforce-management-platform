import { forwardRef, type TextareaHTMLAttributes } from 'react'
import { cn } from '../../lib/utils/cn'

export type TextareaProps = TextareaHTMLAttributes<HTMLTextAreaElement> & {
  invalid?: boolean
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(({ className, invalid, ...props }, ref) => (
  <textarea
    ref={ref}
    aria-invalid={invalid || props['aria-invalid'] || undefined}
    className={cn(
      'min-h-24 w-full resize-y rounded-md border border-border bg-surface px-3 py-2 text-sm text-text-primary transition-colors placeholder:text-text-muted',
      'hover:border-border-strong focus-visible:border-brand focus-visible:outline-brand',
      'disabled:cursor-not-allowed disabled:bg-surface-secondary disabled:text-text-muted read-only:bg-surface-secondary',
      invalid && 'border-destructive focus-visible:border-destructive focus-visible:outline-destructive',
      className,
    )}
    {...props}
  />
))

Textarea.displayName = 'Textarea'
