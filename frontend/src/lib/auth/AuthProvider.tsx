import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { apiRequest, configureApiAuth } from '../api'
import type { AuthSession, AuthUser, LoginRequest, LoginResponse, RefreshResponse } from './authTypes'
import { readAccessTokenClaims } from './jwt'
import { clearStoredSession, readStoredSession, writeStoredSession } from './tokenStorage'

type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated'

type AuthContextValue = {
  status: AuthStatus
  session: AuthSession | null
  user: AuthUser | null
  accessToken: string | null
  login: (request: LoginRequest) => Promise<void>
  logout: () => Promise<void>
  refreshSession: () => Promise<boolean>
  clearSession: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

type AuthProviderProps = {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [session, setSession] = useState<AuthSession | null>(() => readStoredSession())
  const status: AuthStatus = session ? 'authenticated' : 'unauthenticated'

  const clearSession = useCallback(() => {
    clearStoredSession()
    setSession(null)
  }, [])

  const login = useCallback(async (request: LoginRequest) => {
    const response = await apiRequest<LoginResponse>('/auth/login', {
      method: 'POST',
      body: request,
      auth: false,
      retryOnUnauthorized: false,
    })
    const sessionId = response.sessionId || readAccessTokenClaims(response.accessToken).sid
    if (!sessionId) {
      throw new Error('Login response access token did not include a session id claim.')
    }
    const nextSession: AuthSession = { ...response, sessionId }
    writeStoredSession(nextSession)
    setSession(nextSession)
  }, [])

  const logout = useCallback(async () => {
    const activeSession = readStoredSession()
    if (activeSession) {
      await apiRequest<void>('/auth/logout', {
        method: 'POST',
        body: { sessionId: activeSession.sessionId },
        retryOnUnauthorized: false,
      }).catch(() => undefined)
    }
    clearSession()
  }, [clearSession])

  const refreshSession = useCallback(async () => {
    const activeSession = readStoredSession()
    if (!activeSession?.refreshToken) {
      clearSession()
      return false
    }

    const rotation = await apiRequest<RefreshResponse>('/auth/refresh', {
      method: 'POST',
      body: { refreshToken: activeSession.refreshToken },
      auth: false,
      retryOnUnauthorized: false,
    }).catch(() => null)

    if (!rotation) {
      clearSession()
      return false
    }

    const sessionId = rotation.sessionId || readAccessTokenClaims(rotation.accessToken).sid
    if (!sessionId || sessionId !== activeSession.sessionId) {
      clearSession()
      return false
    }

    const rotatedSession: AuthSession = { ...rotation, sessionId }
    writeStoredSession(rotatedSession)
    setSession(rotatedSession)

    return true
  }, [clearSession])

  useEffect(() => {
    configureApiAuth({
      getAccessToken: () => readStoredSession()?.accessToken ?? null,
      refresh: refreshSession,
      clear: clearSession,
    })
  }, [clearSession, refreshSession])

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      session,
      user: session?.user ?? null,
      accessToken: session?.accessToken ?? null,
      login,
      logout,
      refreshSession,
      clearSession,
    }),
    [clearSession, login, logout, refreshSession, session, status],
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
