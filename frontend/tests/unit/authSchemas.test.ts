import { acceptInvitationSchema, resetPasswordSchema } from '../../src/features/auth/authSchemas'

describe('auth password schemas', () => {
  it('matches the backend new-password policy for reset passwords', () => {
    expect(resetPasswordSchema.safeParse({ token: 'token', newPassword: 'short1A', confirmPassword: 'short1A' }).success).toBe(false)
    expect(resetPasswordSchema.safeParse({ token: 'token', newPassword: 'lowercase1', confirmPassword: 'lowercase1' }).success).toBe(false)
    expect(resetPasswordSchema.safeParse({ token: 'token', newPassword: 'UPPERCASE1', confirmPassword: 'UPPERCASE1' }).success).toBe(false)
    expect(resetPasswordSchema.safeParse({ token: 'token', newPassword: 'Password', confirmPassword: 'Password' }).success).toBe(false)
    expect(resetPasswordSchema.safeParse({ token: 'token', newPassword: 'Password1', confirmPassword: 'Password1' }).success).toBe(true)
  })

  it('uses the same password policy for invitation acceptance', () => {
    const base = { token: 'token', displayName: 'Student User', firstName: '', lastName: '', department: '' }

    expect(acceptInvitationSchema.safeParse({ ...base, password: 'Password1', confirmPassword: 'Password2' }).success).toBe(false)
    expect(acceptInvitationSchema.safeParse({ ...base, password: 'Password1', confirmPassword: 'Password1' }).success).toBe(true)
  })
})
