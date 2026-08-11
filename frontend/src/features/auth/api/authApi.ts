import { apiRequest } from '../../../lib/api'
import type {
  AcceptInvitationRequest,
  ForgotPasswordRequest,
  Invitation,
  LoginRequest,
  LoginResponse,
  LogoutRequest,
  PasswordResetRequest,
  PasswordResetResult,
  RefreshResponse,
  SessionRecord,
} from '../../../lib/auth/authTypes'

export function login(request: LoginRequest): Promise<LoginResponse> {
  return apiRequest<LoginResponse>('/auth/login', {
    method: 'POST',
    body: request,
    auth: false,
    retryOnUnauthorized: false,
  })
}

export function refresh(refreshToken: string): Promise<RefreshResponse> {
  return apiRequest<RefreshResponse>('/auth/refresh', {
    method: 'POST',
    body: { refreshToken },
    auth: false,
    retryOnUnauthorized: false,
  })
}

export function logout(request: LogoutRequest): Promise<void> {
  return apiRequest<void>('/auth/logout', {
    method: 'POST',
    body: request,
    retryOnUnauthorized: false,
  })
}

export function forgotPassword(request: ForgotPasswordRequest): Promise<{ email: string; expiresAt: string }> {
  return apiRequest<{ email: string; expiresAt: string }>('/auth/forgot-password', {
    method: 'POST',
    body: request,
    auth: false,
    retryOnUnauthorized: false,
  })
}

export function resetPassword(request: PasswordResetRequest): Promise<PasswordResetResult> {
  return apiRequest<PasswordResetResult>('/auth/reset-password', {
    method: 'POST',
    body: request,
    auth: false,
    retryOnUnauthorized: false,
  })
}

export function acceptInvitation(request: AcceptInvitationRequest): Promise<Invitation> {
  return apiRequest<Invitation>('/invitations/accept', {
    method: 'POST',
    body: request,
    auth: false,
    retryOnUnauthorized: false,
  })
}

export function listSessions(signal?: AbortSignal): Promise<SessionRecord[]> {
  return apiRequest<SessionRecord[]>('/sessions/', { signal })
}

export function revokeSession(sessionId: string): Promise<void> {
  return apiRequest<void>(`/sessions/${sessionId}`, { method: 'DELETE' })
}

export function revokeAllSessions(): Promise<{ revoked: number }> {
  return apiRequest<{ revoked: number }>('/sessions/', { method: 'DELETE' })
}
