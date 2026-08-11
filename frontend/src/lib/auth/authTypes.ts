export type UserRole = 'ADMIN' | 'TASK_MANAGER' | 'REVIEWER' | 'STUDENT'

export type AuthUser = {
  id: string
  email: string
  displayName: string
  roles: UserRole[]
}

export type AuthSession = {
  sessionId: string
  accessToken: string
  refreshToken: string
  expiresAt: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
  sessionExpiresAt: string
  user: AuthUser
}

export type LoginRequest = {
  email: string
  password: string
  deviceName?: string
}

export type LoginResponse = AuthSession

export type RefreshResponse = AuthSession
