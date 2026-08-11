import { useId, type ReactNode } from 'react'
import { cn } from '../../lib/utils/cn'

export type FormFieldProps = {
  label: string
  children: (field: { id: string; describedBy: string | undefined; invalid: boolean }) => ReactNode
  description?: string
  helperText?: string
  error?: string
  required?: boolean
  className?: string
}

export function FormField({ label, children, description, helperText, error, required, className }: FormFieldProps) {
  const generatedId = useId()
  const descriptionId = description ? `${generatedId}-description` : undefined
  const helperId = helperText ? `${generatedId}-helper` : undefined
  const errorId = error ? `${generatedId}-error` : undefined
  const describedBy = [descriptionId, helperId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className={cn('space-y-1.5', className)}>
      <label htmlFor={generatedId} className="block text-sm font-medium text-text-primary">
        {label}
        {required ? <span className="ml-1 text-brand" aria-hidden="true">*</span> : null}
      </label>
      {description ? <p id={descriptionId} className="text-xs text-text-secondary">{description}</p> : null}
      {children({ id: generatedId, describedBy, invalid: Boolean(error) })}
      {helperText ? <p id={helperId} className="text-xs text-text-muted">{helperText}</p> : null}
      {error ? <p id={errorId} className="text-xs font-medium text-destructive">{error}</p> : null}
    </div>
  )
}
