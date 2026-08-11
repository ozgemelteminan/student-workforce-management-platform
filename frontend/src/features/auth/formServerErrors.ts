import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import type { AuthFormError } from './authErrors'

export function applyServerFieldErrors<TValues extends FieldValues>(
  authError: AuthFormError,
  setError: UseFormSetError<TValues>,
): void {
  for (const [field, message] of Object.entries(authError.fieldErrors)) {
    setError(field as Path<TValues>, { type: 'server', message })
  }
}
