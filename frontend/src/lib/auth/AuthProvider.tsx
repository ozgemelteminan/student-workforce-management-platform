import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { login as requestLogin, logout as requestLogout, refresh as requestRefresh } from '../../features/auth/api/authApi'
import { configureApiAuth } from '../api'
import type { AuthSession, AuthUser, LoginRequest } from './authTypes'
import { readAccessTokenClaims } from './jwt'
import { clearStoredSession, readStoredSession, writeStoredSession } from './tokenStorage'

type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated'

type AuthContextValue = {
  status: AuthStatus
  session: AuthSession | null
  user: AuthUser | null
  accessToken: string | null
  unauthenticatedReason: 'expired' | null
  login: (request: LoginRequest) => Promise<void>
  logout: () => Promise<void>
  refreshSession: () => Promise<boolean>
  clearSession: (reason?: 'expired' | 'manual') => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

type AuthProviderProps = {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const queryClient = useQueryClient()
  const [session, setSession] = useState<AuthSession | null>(null)
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [unauthenticatedReason, setUnauthenticatedReason] = useState<'expired' | null>(null)

  const clearUserScopedQueries = useCallback(() => {
    void queryClient.cancelQueries()
    queryClient.removeQueries()
  }, [queryClient])

  const clearSession = useCallback((reason: 'expired' | 'manual' = 'expired') => {
    clearStoredSession()
    setSession(null)
    setStatus('unauthenticated')
    setUnauthenticatedReason(reason === 'expired' ? 'expired' : null)
    clearUserScopedQueries()
  }, [clearUserScopedQueries])

  const commitSession = useCallback((nextSession: AuthSession) => {
    writeStoredSession(nextSession)
    setSession(nextSession)
    setStatus('authenticated')
    setUnauthenticatedReason(null)
  }, [])

  const login = useCallback(async (request: LoginRequest) => {
    clearUserScopedQueries()
    const response = await requestLogin(request)
    const sessionId = response.sessionId || readAccessTokenClaims(response.accessToken).sid
    if (!sessionId) {
      throw new Error('Login response access token did not include a session id claim.')
    }
    const nextSession = normalizeSession(response, sessionId)
    commitSession(nextSession)
  }, [clearUserScopedQueries, commitSession])

  const logout = useCallback(async () => {
    const activeSession = readStoredSession()
    if (activeSession) {
      await requestLogout({ sessionId: activeSession.sessionId }).catch(() => undefined)
    }
    clearSession('manual')
  }, [clearSession])

  const refreshSession = useCallback(async () => {
    const activeSession = readStoredSession()
    if (!activeSession?.refreshToken) {
      clearSession()
      return false
    }

    if (!isFutureInstant(activeSession.refreshTokenExpiresAt)) {
      clearSession()
      return false
    }

    const rotation = await requestRefresh(activeSession.refreshToken).catch(() => null)

    if (!rotation) {
      clearSession()
      return false
    }

    const sessionId = rotation.sessionId || readAccessTokenClaims(rotation.accessToken).sid
    if (!sessionId || sessionId !== activeSession.sessionId) {
      clearSession()
      return false
    }

    const rotatedSession = normalizeSession(rotation, sessionId)
    commitSession(rotatedSession)

    return true
  }, [clearSession, commitSession])

  useEffect(() => {
    const storedSession = readStoredSession()
    if (!storedSession) {
      setStatus('unauthenticated')
      setUnauthenticatedReason(null)
      return
    }

    if (isFutureInstant(storedSession.accessTokenExpiresAt, 30_000) && isFutureInstant(storedSession.sessionExpiresAt)) {
      commitSession(storedSession)
      return
    }

    if (isFutureInstant(storedSession.refreshTokenExpiresAt) && isFutureInstant(storedSession.sessionExpiresAt)) {
      void refreshSession()
      return
    }

    clearSession('expired')
  }, [clearSession, commitSession, refreshSession])

  useEffect(() => {
    configureApiAuth({
      getAccessToken: () => readStoredSession()?.accessToken ?? null,
      refresh: refreshSession,
      clear: () => clearSession('expired'),
    })
  }, [clearSession, refreshSession])

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      session,
      user: session?.user ?? null,
      accessToken: session?.accessToken ?? null,
      unauthenticatedReason,
      login,
      logout,
      refreshSession,
      clearSession,
    }),
    [clearSession, login, logout, refreshSession, session, status, unauthenticatedReason],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.')
  }
  return context
}

function isFutureInstant(value: string, skewMs = 0): boolean {
  const time = Date.parse(value)
  return Number.isFinite(time) && time - skewMs > Date.now()
}

function normalizeSession(response: AuthSession, sessionId: string): AuthSession {
  const accessTokenExpiresAt = response.accessTokenExpiresAt || response.expiresAt
  const sessionExpiresAt = response.sessionExpiresAt || response.expiresAt
  if (!accessTokenExpiresAt || !sessionExpiresAt || !response.refreshTokenExpiresAt) {
    throw new Error('Authentication response did not include required session expiration fields.')
  }

  return {
    ...response,
    sessionId,
    expiresAt: response.expiresAt || accessTokenExpiresAt,
    accessTokenExpiresAt,
    sessionExpiresAt,
  }
}
