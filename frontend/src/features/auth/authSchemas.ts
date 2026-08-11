import { z } from 'zod'

const email = z.string().trim().min(1, 'Email is required.').email('Enter a valid email address.').max(256, 'Email must be 256 characters or fewer.')
const token = z.string().trim().min(1, 'Token is required.').max(2048, 'Token is too long.')

export const passwordRequirements = [
  'At least 8 characters',
  'At least one uppercase letter',
  'At least one lowercase letter',
  'At least one number',
] as const

export const loginSchema = z.object({
  email,
  password: z.string().min(1, 'Password is required.').max(1024, 'Password must be 1024 characters or fewer.'),
  deviceName: z.string().trim().max(200, 'Device name must be 200 characters or fewer.').optional(),
})

export const forgotPasswordSchema = z.object({ email })

export const newPasswordSchema = z
  .string()
  .min(8, 'Password must be at least 8 characters.')
  .max(256, 'Password must be 256 characters or fewer.')
  .regex(/[A-Z]/, 'Password must contain an uppercase letter.')
  .regex(/[a-z]/, 'Password must contain a lowercase letter.')
  .regex(/[0-9]/, 'Password must contain a number.')

export const resetPasswordSchema = z
  .object({
    token,
    newPassword: newPasswordSchema,
    confirmPassword: z.string().min(1, 'Confirm your new password.'),
  })
  .refine((value) => value.newPassword === value.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Passwords must match.',
  })

export const acceptInvitationSchema = z
  .object({
    token,
    displayName: z.string().trim().min(1, 'Display name is required.').max(200, 'Display name must be 200 characters or fewer.'),
    firstName: z.string().trim().max(120, 'First name must be 120 characters or fewer.').optional(),
    lastName: z.string().trim().max(120, 'Last name must be 120 characters or fewer.').optional(),
    department: z.string().trim().max(160, 'Department must be 160 characters or fewer.').optional(),
    password: newPasswordSchema,
    confirmPassword: z.string().min(1, 'Confirm your password.'),
  })
  .refine((value) => value.password === value.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Passwords must match.',
  })

export type LoginFormValues = z.infer<typeof loginSchema>
export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>
export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>
export type AcceptInvitationFormValues = z.infer<typeof acceptInvitationSchema>
