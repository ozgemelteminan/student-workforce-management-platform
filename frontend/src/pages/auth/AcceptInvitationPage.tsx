import { zodResolver } from '@hookform/resolvers/zod'
import { UserPlus } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Button, FormField, Input } from '../../components/ui'
import { acceptInvitation } from '../../features/auth/api/authApi'
import { AuthFormMessage } from '../../features/auth/components/AuthFormMessage'
import { AuthLayout } from '../../features/auth/components/AuthLayout'
import { PasswordInput } from '../../features/auth/components/PasswordInput'
import { PasswordRequirements } from '../../features/auth/components/PasswordRequirements'
import { toTokenWorkflowError } from '../../features/auth/authErrors'
import { acceptInvitationSchema, type AcceptInvitationFormValues } from '../../features/auth/authSchemas'
import { applyServerFieldErrors } from '../../features/auth/formServerErrors'
import { appToast } from '../../lib/toast'

export function AcceptInvitationPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const initialToken = useMemo(() => searchParams.get('token') ?? '', [searchParams])
  const [acceptedEmail, setAcceptedEmail] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [retryAfterSeconds, setRetryAfterSeconds] = useState<number | undefined>()
  const form = useForm<AcceptInvitationFormValues>({
    resolver: zodResolver(acceptInvitationSchema),
    defaultValues: {
      token: initialToken,
      displayName: '',
      firstName: '',
      lastName: '',
      department: '',
      password: '',
      confirmPassword: '',
    },
  })
  const mutation = useMutation({ mutationFn: acceptInvitation })

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    setRetryAfterSeconds(undefined)
    try {
      const response = await mutation.mutateAsync({
        token: values.token,
        displayName: values.displayName,
        password: values.password,
        firstName: values.firstName?.trim() || null,
        lastName: values.lastName?.trim() || null,
        department: values.department?.trim() || null,
      })
      setAcceptedEmail(response.email)
      appToast.success('Invitation accepted.')
      navigate('/invitations/accept', { replace: true })
    } catch (error) {
      const authError = toTokenWorkflowError(error, 'invitation')
      applyServerFieldErrors(authError, form.setError)
      setFormError(authError.formMessage)
      setRetryAfterSeconds(authError.retryAfterSeconds)
      const firstErrorField = Object.keys(authError.fieldErrors)[0] as keyof AcceptInvitationFormValues | undefined
      form.setFocus(firstErrorField ?? 'token')
    }
  })

  return (
    <AuthLayout title="Accept invitation" description="Create your password to activate your invited account.">
      {acceptedEmail ? (
        <div className="space-y-4">
          <div className="rounded-md border border-border bg-surface-secondary px-3 py-3 text-sm text-text-secondary">
            <p className="font-medium text-text-primary">Your invitation has been accepted.</p>
            <p className="mt-1 break-words">{acceptedEmail}</p>
            <p className="mt-1">Sign in with your new password to continue.</p>
          </div>
          <Button variant="primary" className="w-full" onClick={() => navigate('/login', { replace: true })}>
            Back to sign in
          </Button>
        </div>
      ) : (
        <form className="space-y-4" onSubmit={onSubmit} noValidate>
          <AuthFormMessage message={formError} retryAfterSeconds={retryAfterSeconds} />
          <FormField label="Invitation token" error={form.formState.errors.token?.message} required>
            {({ id, describedBy, invalid }) => (
              <Input id={id} autoComplete="one-time-code" invalid={invalid} aria-describedby={describedBy} {...form.register('token')} />
            )}
          </FormField>
          <FormField label="Display name" error={form.formState.errors.displayName?.message} required>
            {({ id, describedBy, invalid }) => (
              <Input id={id} autoComplete="name" invalid={invalid} aria-describedby={describedBy} {...form.register('displayName')} />
            )}
          </FormField>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="First name" error={form.formState.errors.firstName?.message}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} autoComplete="given-name" invalid={invalid} aria-describedby={describedBy} {...form.register('firstName')} />
              )}
            </FormField>
            <FormField label="Last name" error={form.formState.errors.lastName?.message}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} autoComplete="family-name" invalid={invalid} aria-describedby={describedBy} {...form.register('lastName')} />
              )}
            </FormField>
          </div>
          <FormField label="Department" error={form.formState.errors.department?.message}>
            {({ id, describedBy, invalid }) => (
              <Input id={id} autoComplete="organization" invalid={invalid} aria-describedby={describedBy} {...form.register('department')} />
            )}
          </FormField>
          <FormField label="Password" error={form.formState.errors.password?.message} required>
            {({ id, describedBy, invalid }) => (
              <PasswordInput
                id={id}
                autoComplete="new-password"
                invalid={invalid}
                aria-describedby={describedBy}
                {...form.register('password')}
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
            <Button type="submit" isLoading={mutation.isPending} iconBefore={<UserPlus aria-hidden="true" className="h-4 w-4" />}>
              Accept invitation
            </Button>
          </div>
        </form>
      )}
    </AuthLayout>
  )
}
