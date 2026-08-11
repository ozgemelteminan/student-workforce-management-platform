import { AlertCircle } from 'lucide-react'

type AuthFormMessageProps = {
  message: string | null
  retryAfterSeconds?: number
}

export function AuthFormMessage({ message, retryAfterSeconds }: AuthFormMessageProps) {
  if (!message) {
    return null
  }

  return (
    <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
      <div className="flex gap-2">
        <AlertCircle aria-hidden="true" className="mt-0.5 h-4 w-4 shrink-0" />
        <div>
          <p>{message}</p>
          {retryAfterSeconds ? <p className="mt-1 text-xs">Retry after {retryAfterSeconds} seconds.</p> : null}
        </div>
      </div>
    </div>
  )
}
