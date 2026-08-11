import { zodResolver } from '@hookform/resolvers/zod'
import { Mail } from 'lucide-react'
import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { Button, FormField, Input } from '../../components/ui'
import { forgotPassword } from '../../features/auth/api/authApi'
import { AuthFormMessage } from '../../features/auth/components/AuthFormMessage'
import { AuthLayout } from '../../features/auth/components/AuthLayout'
import { toForgotPasswordError } from '../../features/auth/authErrors'
import { forgotPasswordSchema, type ForgotPasswordFormValues } from '../../features/auth/authSchemas'
import { applyServerFieldErrors } from '../../features/auth/formServerErrors'
import { defaultAuthenticatedPath } from '../../features/auth/returnTo'
import { formatIstanbulDateTime } from '../../lib/date-time'
import { useAuth } from '../../lib/auth/AuthProvider'

export function ForgotPasswordPage() {
  const auth = useAuth()
  const [submittedEmail, setSubmittedEmail] = useState<string | null>(null)
  const [expiresAt, setExpiresAt] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [retryAfterSeconds, setRetryAfterSeconds] = useState<number | undefined>()
  const navigate = useNavigate()
  const form = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  })
  const mutation = useMutation({ mutationFn: forgotPassword })

  if (auth.status === 'authenticated') {
    return <Navigate to={defaultAuthenticatedPath} replace />
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    setRetryAfterSeconds(undefined)
    try {
      const response = await mutation.mutateAsync({ email: values.email })
      setSubmittedEmail(values.email)
      setExpiresAt(response.expiresAt)
    } catch (error) {
      const authError = toForgotPasswordError(error)
      applyServerFieldErrors(authError, form.setError)
      setFormError(authError.formMessage)
      setRetryAfterSeconds(authError.retryAfterSeconds)
      const firstErrorField = Object.keys(authError.fieldErrors)[0] as keyof ForgotPasswordFormValues | undefined
      form.setFocus(firstErrorField ?? 'email')
    }
  })

  return (
    <AuthLayout title="Reset your password" description="Enter your email address to start a password reset.">
      {submittedEmail ? (
        <div className="space-y-4">
          <div className="rounded-md border border-border bg-surface-secondary px-3 py-3 text-sm text-text-secondary">
            <p className="font-medium text-text-primary">If an account exists for this email, password reset instructions have been sent.</p>
            <p className="mt-1 break-words">{submittedEmail}</p>
            {expiresAt ? <p className="mt-1">Reset links expire at {formatIstanbulDateTime(expiresAt)}.</p> : null}
          </div>
          <Button variant="outline" className="w-full" onClick={() => navigate('/login', { replace: true })}>
            Back to sign in
          </Button>
        </div>
      ) : (
        <form className="space-y-4" onSubmit={onSubmit} noValidate>
          <AuthFormMessage message={formError} retryAfterSeconds={retryAfterSeconds} />
          <FormField label="Email" error={form.formState.errors.email?.message} required>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                autoComplete="email"
                inputMode="email"
                invalid={invalid}
                aria-describedby={describedBy}
                {...form.register('email')}
              />
            )}
          </FormField>
          <div className="flex items-center justify-between gap-3">
            <Link to="/login" className="text-sm font-medium text-brand hover:text-brand-hover">
              Back to sign in
            </Link>
            <Button type="submit" isLoading={mutation.isPending} iconBefore={<Mail aria-hidden="true" className="h-4 w-4" />}>
              Send reset
            </Button>
          </div>
        </form>
      )}
    </AuthLayout>
  )
}
