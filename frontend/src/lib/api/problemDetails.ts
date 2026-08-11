export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  traceId?: string
  errors?: Record<string, string[]>
  retryAfterSeconds?: number
}

export type NormalizedApiError = {
  status: number
  title: string
  detail?: string
  traceId?: string
  validationErrors: Record<string, string[]>
  retryAfterSeconds?: number
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: NormalizedApiError

  constructor(problem: NormalizedApiError) {
    super(problem.title)
    this.name = 'ApiError'
    this.status = problem.status
    this.problem = problem
  }
}

const fallbackTitles: Record<number, string> = {
  401: 'Authentication required',
  403: 'Permission denied',
  404: 'Resource not found',
  409: 'Conflict',
  422: 'Validation failed',
  429: 'Too many requests',
  500: 'Unexpected error',
}

export function normalizeProblemDetails(status: number, payload: unknown): NormalizedApiError {
  const problem = isProblemRecord(payload) ? payload : {}
  const errors = isValidationErrors(problem.errors) ? problem.errors : {}
  return {
    status,
    title: typeof problem.title === 'string' ? problem.title : fallbackTitles[status] ?? 'Request failed',
    detail: typeof problem.detail === 'string' ? problem.detail : undefined,
    traceId: typeof problem.traceId === 'string' ? problem.traceId : undefined,
    validationErrors: errors,
    retryAfterSeconds: typeof problem.retryAfterSeconds === 'number' ? problem.retryAfterSeconds : undefined,
  }
}

function isProblemRecord(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null
}

function isValidationErrors(value: unknown): value is Record<string, string[]> {
  if (typeof value !== 'object' || value === null) {
    return false
  }

  return Object.values(value).every(
    (messages) => Array.isArray(messages) && messages.every((message) => typeof message === 'string'),
  )
}

