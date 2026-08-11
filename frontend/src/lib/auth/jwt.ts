export type AccessTokenClaims = {
  sid?: string
  exp?: number
}

export function readAccessTokenClaims(token: string): AccessTokenClaims {
  const [, payload] = token.split('.')
  if (!payload) {
    return {}
  }

  try {
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/')
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=')
    return JSON.parse(atob(padded)) as AccessTokenClaims
  } catch {
    return {}
  }
}

