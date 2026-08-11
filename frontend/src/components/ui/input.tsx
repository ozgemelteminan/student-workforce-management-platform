import { Search, X } from 'lucide-react'
import { forwardRef, type InputHTMLAttributes } from 'react'
import { cn } from '../../lib/utils/cn'
import { IconButton } from './icon-button'

export type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  invalid?: boolean
}

export const Input = forwardRef<HTMLInputElement, InputProps>(({ className, invalid, ...props }, ref) => (
  <input
    ref={ref}
    aria-invalid={invalid || props['aria-invalid'] || undefined}
    className={cn(
      'h-9 w-full rounded-md border border-border bg-surface px-3 text-sm text-text-primary shadow-none transition-colors placeholder:text-text-muted',
      'hover:border-border-strong focus-visible:border-brand focus-visible:outline-brand',
      'disabled:cursor-not-allowed disabled:bg-surface-secondary disabled:text-text-muted',
      'read-only:bg-surface-secondary',
      invalid && 'border-destructive focus-visible:border-destructive focus-visible:outline-destructive',
      className,
    )}
    {...props}
  />
))

Input.displayName = 'Input'

export type SearchInputProps = Omit<InputProps, 'type'> & {
  label: string
  onClear?: () => void
}

export const SearchInput = forwardRef<HTMLInputElement, SearchInputProps>(({ className, label, value, onClear, placeholder = 'Search', ...props }, ref) => (
  <div className="relative">
    <Search aria-hidden="true" className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-text-muted" />
    <Input
      ref={ref}
      type="search"
      aria-label={label}
      value={value}
      placeholder={placeholder}
      className={cn('pl-9', onClear && value ? 'pr-10' : undefined, className)}
      {...props}
    />
    {onClear && value ? (
      <IconButton
        type="button"
        label="Clear search"
        icon={<X aria-hidden="true" className="h-4 w-4" />}
        className="absolute right-1 top-1/2 h-7 w-7 -translate-y-1/2"
        onClick={onClear}
      />
    ) : null}
  </div>
))

SearchInput.displayName = 'SearchInput'
