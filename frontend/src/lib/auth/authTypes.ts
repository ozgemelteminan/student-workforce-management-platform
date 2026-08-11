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
  deviceName: string | null
}

export type LoginResponse = AuthSession

export type RefreshResponse = AuthSession

export type LogoutRequest = {
  sessionId: string
}

export type ForgotPasswordRequest = {
  email: string
}

export type PasswordResetRequest = {
  token: string
  newPassword: string
}

export type PasswordResetResult = {
  userId: string
  consumedAt: string
  revokedSessionCount: number
}

export type AcceptInvitationRequest = {
  token: string
  password: string
  displayName: string
  firstName: string | null
  lastName: string | null
  department: string | null
}

export type Invitation = {
  id: string
  email: string
  expiresAt: string
  acceptedAt: string | null
  revokedAt: string | null
  createdById: string | null
  createdAt: string
}

export type SessionRecord = {
  id: string
  userId: string
  deviceName: string | null
  ipAddress: string | null
  expiresAt: string
  revokedAt: string | null
  createdAt: string
}
