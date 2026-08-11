import { zodResolver } from '@hookform/resolvers/zod'
import { LogIn } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { Button, FormField, Input } from '../../components/ui'
import { AuthFormMessage } from '../../features/auth/components/AuthFormMessage'
import { AuthLayout } from '../../features/auth/components/AuthLayout'
import { PasswordInput } from '../../features/auth/components/PasswordInput'
import { toLoginError } from '../../features/auth/authErrors'
import { loginSchema, type LoginFormValues } from '../../features/auth/authSchemas'
import { applyServerFieldErrors } from '../../features/auth/formServerErrors'
import { returnToFromLocationState, sanitizeReturnTo } from '../../features/auth/returnTo'
import { useAuth } from '../../lib/auth/AuthProvider'

export function LoginPage() {
  const auth = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)
  const [retryAfterSeconds, setRetryAfterSeconds] = useState<number | undefined>()
  const returnTo = sanitizeReturnTo(new URLSearchParams(location.search).get('returnTo') ?? returnToFromLocationState(location.state))
  const sessionExpired = auth.unauthenticatedReason === 'expired'

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '', deviceName: '' },
  })

  if (auth.status === 'authenticated') {
    return <Navigate to={returnTo} replace />
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    setRetryAfterSeconds(undefined)
    try {
      await auth.login({
        email: values.email,
        password: values.password,
        deviceName: values.deviceName?.trim() || null,
      })
      navigate(returnTo, { replace: true })
    } catch (error) {
      const authError = toLoginError(error)
      applyServerFieldErrors(authError, form.setError)
      setFormError(authError.formMessage)
      setRetryAfterSeconds(authError.retryAfterSeconds)
      const firstErrorField = Object.keys(authError.fieldErrors)[0] as keyof LoginFormValues | undefined
      form.setFocus(firstErrorField ?? 'password')
    }
  })

  return (
    <AuthLayout
      title="Sign in"
      description="Use your university workforce account to continue."
      footer={<span>Need access? Ask an administrator for an invitation.</span>}
    >
      <form className="space-y-4" onSubmit={onSubmit} noValidate>
        {sessionExpired ? (
          <div className="rounded-md border border-border bg-surface-secondary px-3 py-2 text-sm text-text-secondary">
            Your session has expired. Sign in again to continue.
          </div>
        ) : null}
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
        <FormField label="Password" error={form.formState.errors.password?.message} required>
          {({ id, describedBy, invalid }) => (
            <PasswordInput
              id={id}
              autoComplete="current-password"
              invalid={invalid}
              aria-describedby={describedBy}
              {...form.register('password')}
            />
          )}
        </FormField>
        <FormField label="Device name" helperText="Optional. Helps identify this session later." error={form.formState.errors.deviceName?.message}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} autoComplete="off" invalid={invalid} aria-describedby={describedBy} {...form.register('deviceName')} />
          )}
        </FormField>
        <div className="flex items-center justify-between gap-3">
          <Link to="/forgot-password" className="text-sm font-medium text-brand hover:text-brand-hover">
            Forgot password?
          </Link>
          <Button type="submit" isLoading={form.formState.isSubmitting} iconBefore={<LogIn aria-hidden="true" className="h-4 w-4" />}>
            Sign in
          </Button>
        </div>
      </form>
    </AuthLayout>
  )
}
