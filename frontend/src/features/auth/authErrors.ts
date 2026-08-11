import { ApiError } from '../../lib/api'

export type AuthFormError = {
  formMessage: string
  fieldErrors: Record<string, string>
  retryAfterSeconds?: number
}

const fieldAliases: Record<string, string> = {
  email: 'email',
  password: 'password',
  deviceName: 'deviceName',
  rawToken: 'token',
  token: 'token',
  rawResetToken: 'token',
  newPassword: 'newPassword',
  displayName: 'displayName',
  firstName: 'firstName',
  lastName: 'lastName',
  department: 'department',
}

export function toLoginError(error: unknown): AuthFormError {
  if (!(error instanceof ApiError)) {
    return systemError()
  }

  if (error.status === 401 || error.status === 403) {
    return { formMessage: 'Invalid email or password.', fieldErrors: {} }
  }

  if (error.status === 429) {
    return {
      formMessage: 'Too many login attempts. Please try again later.',
      fieldErrors: {},
      retryAfterSeconds: error.problem.retryAfterSeconds,
    }
  }

  return fromValidationOrFallback(error, 'Unable to sign in right now. Please try again.')
}

export function toForgotPasswordError(error: unknown): AuthFormError {
  if (!(error instanceof ApiError)) {
    return systemError()
  }

  if (error.status === 429) {
    return {
      formMessage: 'Too many password reset requests. Please try again later.',
      fieldErrors: {},
      retryAfterSeconds: error.problem.retryAfterSeconds,
    }
  }

  return fromValidationOrFallback(error, 'Unable to request a password reset right now. Please try again.')
}

export function toTokenWorkflowError(error: unknown, workflow: 'reset' | 'invitation'): AuthFormError {
  if (!(error instanceof ApiError)) {
    return systemError()
  }

  if (error.status === 404 || error.status === 409) {
    const message =
      workflow === 'reset'
        ? 'This reset link is invalid, expired, or already used.'
        : 'This invitation is invalid, expired, already accepted, or no longer active.'
    return { formMessage: message, fieldErrors: {} }
  }

  if (error.status === 429) {
    return {
      formMessage: 'Too many attempts. Please try again later.',
      fieldErrors: {},
      retryAfterSeconds: error.problem.retryAfterSeconds,
    }
  }

  return fromValidationOrFallback(error, 'Unable to complete this request right now. Please try again.')
}

function fromValidationOrFallback(error: ApiError, fallback: string): AuthFormError {
  const fieldErrors = mapFieldErrors(error.problem.validationErrors)
  const hasFieldErrors = Object.keys(fieldErrors).length > 0
  return {
    formMessage: hasFieldErrors ? 'Please review the highlighted fields.' : fallback,
    fieldErrors,
    retryAfterSeconds: error.problem.retryAfterSeconds,
  }
}

function mapFieldErrors(errors: Record<string, string[]>): Record<string, string> {
  return Object.fromEntries(
    Object.entries(errors)
      .map(([key, messages]) => {
        const normalized = key.split('.').pop()?.replace(/^\w/, (char) => char.toLowerCase()) ?? key
        const alias = fieldAliases[normalized] ?? fieldAliases[key] ?? normalized
        const message = messages.find(Boolean)
        return message ? [alias, message] : null
      })
      .filter((entry): entry is [string, string] => Boolean(entry)),
  )
}

function systemError(): AuthFormError {
  return { formMessage: 'Unable to reach the authentication service. Please try again.', fieldErrors: {} }
}
