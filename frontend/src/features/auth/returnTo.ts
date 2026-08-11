import type { Location } from 'react-router-dom'

export const defaultAuthenticatedPath = '/'

const authPaths = new Set(['/login', '/forgot-password', '/reset-password', '/invitations/accept'])

export function sanitizeReturnTo(candidate: unknown, fallback = defaultAuthenticatedPath): string {
  if (typeof candidate !== 'string' || candidate.length === 0 || candidate.length > 2048) {
    return fallback
  }

  if (!candidate.startsWith('/') || candidate.startsWith('//') || candidate.includes('\\')) {
    return fallback
  }

  try {
    const url = new URL(candidate, window.location.origin)
    if (url.origin !== window.location.origin || !url.pathname.startsWith('/')) {
      return fallback
    }

    if (authPaths.has(url.pathname)) {
      return fallback
    }

    return `${url.pathname}${url.search}${url.hash}`
  } catch {
    return fallback
  }
}

export function returnToFromLocationState(state: unknown): string {
  const from = typeof state === 'object' && state !== null && 'from' in state ? (state as { from?: Partial<Location> }).from : undefined
  if (!from?.pathname) {
    return defaultAuthenticatedPath
  }

  return sanitizeReturnTo(`${from.pathname}${from.search ?? ''}${from.hash ?? ''}`)
}
