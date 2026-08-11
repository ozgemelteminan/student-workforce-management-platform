import { zodResolver } from '@hookform/resolvers/zod'
import { KeyRound } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Button, FormField, Input } from '../../components/ui'
import { resetPassword } from '../../features/auth/api/authApi'
import { AuthFormMessage } from '../../features/auth/components/AuthFormMessage'
import { AuthLayout } from '../../features/auth/components/AuthLayout'
import { PasswordInput } from '../../features/auth/components/PasswordInput'
import { PasswordRequirements } from '../../features/auth/components/PasswordRequirements'
import { toTokenWorkflowError } from '../../features/auth/authErrors'
import { resetPasswordSchema, type ResetPasswordFormValues } from '../../features/auth/authSchemas'
import { applyServerFieldErrors } from '../../features/auth/formServerErrors'
import { appToast } from '../../lib/toast'

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const initialToken = useMemo(() => searchParams.get('token') ?? '', [searchParams])
  const [success, setSuccess] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [retryAfterSeconds, setRetryAfterSeconds] = useState<number | undefined>()
  const form = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { token: initialToken, newPassword: '', confirmPassword: '' },
  })
  const mutation = useMutation({ mutationFn: resetPassword })

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    setRetryAfterSeconds(undefined)
    try {
      await mutation.mutateAsync({ token: values.token, newPassword: values.newPassword })
      setSuccess(true)
      appToast.success('Password updated.')
      navigate('/reset-password', { replace: true })
    } catch (error) {
      const authError = toTokenWorkflowError(error, 'reset')
      applyServerFieldErrors(authError, form.setError)
      setFormError(authError.formMessage)
      setRetryAfterSeconds(authError.retryAfterSeconds)
      const firstErrorField = Object.keys(authError.fieldErrors)[0] as keyof ResetPasswordFormValues | undefined
      form.setFocus(firstErrorField ?? 'token')
    }
  })

  return (
    <AuthLayout title="Set a new password" description="Use the reset token from your password reset email.">
      {success ? (
        <div className="space-y-4">
          <div className="rounded-md border border-border bg-surface-secondary px-3 py-3 text-sm text-text-secondary">
            <p className="font-medium text-text-primary">Your password has been updated.</p>
            <p className="mt-1">Sign in with your new password to continue.</p>
          </div>
          <Button variant="primary" className="w-full" onClick={() => navigate('/login', { replace: true })}>
            Back to sign in
          </Button>
        </div>
      ) : (
        <form className="space-y-4" onSubmit={onSubmit} noValidate>
          <AuthFormMessage message={formError} retryAfterSeconds={retryAfterSeconds} />
          <FormField label="Reset token" error={form.formState.errors.token?.message} required>
            {({ id, describedBy, invalid }) => (
              <Input id={id} autoComplete="one-time-code" invalid={invalid} aria-describedby={describedBy} {...form.register('token')} />
            )}
          </FormField>
          <FormField label="New password" error={form.formState.errors.newPassword?.message} required>
            {({ id, describedBy, invalid }) => (
              <PasswordInput
                id={id}
                autoComplete="new-password"
                invalid={invalid}
                aria-describedby={describedBy}
                visibilityLabel="new password"
                {...form.register('newPassword')}
              />
            )}
          </FormField>
          <FormField label="Confirm password" error={form.formState.errors.confirmPassword?.message} required>
            {({ id, describedBy, invalid }) => (
              <PasswordInput
                id={id}
                autoComplete="new-password"
                invalid={invalid}
                aria-describedby={describedBy}
                visibilityLabel="password confirmation"
                {...form.register('confirmPassword')}
              />
            )}
          </FormField>
          <PasswordRequirements />
          <div className="flex items-center justify-between gap-3">
            <Link to="/login" className="text-sm font-medium text-brand hover:text-brand-hover">
              Back to sign in
            </Link>
            <Button type="submit" isLoading={mutation.isPending} iconBefore={<KeyRound aria-hidden="true" className="h-4 w-4" />}>
              Update password
            </Button>
          </div>
        </form>
      )}
    </AuthLayout>
  )
}
