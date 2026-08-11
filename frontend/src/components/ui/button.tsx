import { Loader2 } from 'lucide-react'
import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react'
import { cn } from '../../lib/utils/cn'

type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'destructive'
type ButtonSize = 'sm' | 'md' | 'lg' | 'icon'

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant
  size?: ButtonSize
  isLoading?: boolean
  iconBefore?: ReactNode
  iconAfter?: ReactNode
}

const variantClasses: Record<ButtonVariant, string> = {
  primary: 'border-brand bg-brand text-text-inverse hover:bg-brand-hover',
  secondary: 'border-border bg-surface-secondary text-text-primary hover:bg-[#ebe5dd]',
  outline: 'border-border bg-surface text-text-primary hover:border-border-strong hover:bg-surface-secondary',
  ghost: 'border-transparent bg-transparent text-text-secondary hover:bg-surface-secondary hover:text-text-primary',
  destructive: 'border-destructive bg-destructive text-text-inverse hover:bg-[#b91c1c]',
}

const sizeClasses: Record<ButtonSize, string> = {
  sm: 'h-8 gap-1.5 px-3 text-xs',
  md: 'h-9 gap-2 px-4 text-sm',
  lg: 'h-10 gap-2.5 px-5 text-sm',
  icon: 'h-9 w-9 p-0',
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'primary', size = 'md', isLoading = false, iconBefore, iconAfter, children, disabled, type = 'button', ...props }, ref) => (
    <button
      ref={ref}
      type={type}
      disabled={disabled || isLoading}
      className={cn(
        'inline-flex shrink-0 items-center justify-center rounded-md border font-medium transition-colors duration-150',
        'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand',
        'disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-55',
        'active:translate-y-px motion-reduce:active:translate-y-0',
        variantClasses[variant],
        sizeClasses[size],
        className,
      )}
      {...props}
    >
      {isLoading ? <Loader2 aria-hidden="true" className="h-4 w-4 animate-spin motion-reduce:animate-none" /> : iconBefore}
      {children}
      {iconAfter}
    </button>
  ),
)

Button.displayName = 'Button'
