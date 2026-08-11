import { forwardRef } from 'react'
import { Button, type ButtonProps } from './button'
import { WithTooltip } from './tooltip'

export type IconButtonProps = Omit<ButtonProps, 'children' | 'iconBefore' | 'iconAfter'> & {
  label: string
  icon: React.ReactNode
  tooltip?: string
}

export const IconButton = forwardRef<HTMLButtonElement, IconButtonProps>(
  ({ label, icon, tooltip, 'aria-label': ariaLabel, size = 'icon', variant = 'ghost', ...props }, ref) => {
    const button = (
      <Button ref={ref} size={size} variant={variant} aria-label={ariaLabel ?? label} {...props}>
        {icon}
      </Button>
    )

    return tooltip ? <WithTooltip label={tooltip}>{button}</WithTooltip> : button
  },
)

IconButton.displayName = 'IconButton'
