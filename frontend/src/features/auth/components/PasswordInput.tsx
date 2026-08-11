import { Eye, EyeOff } from 'lucide-react'
import { forwardRef, useState, type InputHTMLAttributes } from 'react'
import { Input } from '../../../components/ui'
import { IconButton } from '../../../components/ui/icon-button'

type PasswordInputProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> & {
  invalid?: boolean
  visibilityLabel?: string
}

export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(({ invalid, visibilityLabel = 'password', className, ...props }, ref) => {
  const [visible, setVisible] = useState(false)

  return (
    <div className="relative">
      <Input
        ref={ref}
        type={visible ? 'text' : 'password'}
        invalid={invalid}
        className={className ? `${className} pr-10` : 'pr-10'}
        {...props}
      />
      <IconButton
        type="button"
        label={visible ? `Hide ${visibilityLabel}` : `Show ${visibilityLabel}`}
        icon={visible ? <EyeOff aria-hidden="true" className="h-4 w-4" /> : <Eye aria-hidden="true" className="h-4 w-4" />}
        className="absolute right-1 top-1/2 h-7 w-7 -translate-y-1/2"
        onMouseDown={(event) => event.preventDefault()}
        onClick={() => setVisible((current) => !current)}
      />
    </div>
  )
})

PasswordInput.displayName = 'PasswordInput'
