import { env } from '../env'
import { ApiError, normalizeProblemDetails } from './problemDetails'

type ApiAuthBridge = {
  getAccessToken: () => string | null
  refresh: () => Promise<boolean>
  clear: () => void
}

type ApiRequestOptions = Omit<RequestInit, 'body' | 'headers'> & {
  body?: unknown
  headers?: HeadersInit
  auth?: boolean
  retryOnUnauthorized?: boolean
}

let authBridge: ApiAuthBridge | null = null
let inFlightRefresh: Promise<boolean> | null = null

export function configureApiAuth(bridge: ApiAuthBridge) {
  authBridge = bridge
}

export function buildApiUrl(path: string): string {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return `${env.apiBaseUrl}${env.apiVersion}${normalizedPath}`
}

export async function apiRequest<TResponse>(path: string, options: ApiRequestOptions = {}): Promise<TResponse> {
  return send<TResponse>(path, options, false)
}

async function send<TResponse>(
  path: string,
  options: ApiRequestOptions,
  hasRetriedAfterRefresh: boolean,
): Promise<TResponse> {
  const request = createRequest(path, options)
  const response = await fetch(request)

  if (
    response.status === 401 &&
    options.auth !== false &&
    options.retryOnUnauthorized !== false &&
    !hasRetriedAfterRefresh &&
    authBridge?.getAccessToken()
  ) {
    const refreshed = await refreshOnce()
    if (refreshed) {
      return send<TResponse>(path, options, true)
    }

    authBridge?.clear()
  }

  if (!response.ok) {
    throw new ApiError(normalizeProblemDetails(response.status, await parseResponsePayload(response)))
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  return (await parseResponsePayload(response)) as TResponse
}

async function refreshOnce(): Promise<boolean> {
  inFlightRefresh ??= authBridge
    ? authBridge.refresh().finally(() => {
        inFlightRefresh = null
      })
    : Promise.resolve(false)

  return inFlightRefresh
}

function createRequest(path: string, options: ApiRequestOptions): Request {
  const headers = new Headers(options.headers)
  const token = options.auth === false ? null : authBridge?.getAccessToken()

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  if (options.body !== undefined && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }

  return new Request(buildApiUrl(path), {
    ...options,
    headers,
    body: options.body === undefined || options.body instanceof FormData ? options.body : JSON.stringify(options.body),
  })
}

async function parseResponsePayload(response: Response): Promise<unknown> {
  const contentType = response.headers.get('content-type') ?? ''
  if (!contentType.includes('application/json')) {
    return undefined
  }

  return response.json()
}

