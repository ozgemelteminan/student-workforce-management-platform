import type { AuthSession } from './authTypes'

const storageKey = 'swm.auth.session'

export function readStoredSession(): AuthSession | null {
  const raw = sessionStorage.getItem(storageKey)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as AuthSession
  } catch {
    sessionStorage.removeItem(storageKey)
    return null
  }
}

export function writeStoredSession(session: AuthSession): void {
  sessionStorage.setItem(storageKey, JSON.stringify(session))
}

export function clearStoredSession(): void {
  sessionStorage.removeItem(storageKey)
}

