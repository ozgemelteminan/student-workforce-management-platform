import { normalizeProblemDetails } from '../../src/lib/api'

describe('normalizeProblemDetails', () => {
  it('preserves safe ProblemDetails fields and validation errors', () => {
    const problem = normalizeProblemDetails(422, {
      title: 'Validation failed',
      detail: 'One or more validation errors occurred.',
      traceId: 'trace-1',
      errors: {
        deadline: ['Deadline must be later than the current date.'],
      },
    })

    expect(problem).toEqual({
      status: 422,
      title: 'Validation failed',
      detail: 'One or more validation errors occurred.',
      traceId: 'trace-1',
      validationErrors: {
        deadline: ['Deadline must be later than the current date.'],
      },
      retryAfterSeconds: undefined,
    })
  })

  it('uses safe fallback titles for non-json failures', () => {
    expect(normalizeProblemDetails(403, undefined).title).toBe('Permission denied')
  })
})

