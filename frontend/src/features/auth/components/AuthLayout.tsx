import type { ReactNode } from 'react'

type AuthLayoutProps = {
  title: string
  description?: string
  children: ReactNode
  footer?: ReactNode
}

export function AuthLayout({ title, description, children, footer }: AuthLayoutProps) {
  return (
    <main className="flex min-h-screen items-center justify-center bg-page px-4 py-8 text-text-primary">
      <section className="w-full max-w-md">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-brand text-sm font-semibold text-text-inverse">
            SW
          </div>
          <p className="text-xs font-semibold uppercase text-brand">Student Workforce Management</p>
          <h1 className="mt-2 text-xl font-semibold text-text-primary">{title}</h1>
          {description ? <p className="mt-2 text-sm text-text-secondary">{description}</p> : null}
        </div>
        <div className="rounded-lg border border-border bg-surface p-5 shadow-sm sm:p-6">{children}</div>
        {footer ? <div className="mt-4 text-center text-sm text-text-secondary">{footer}</div> : null}
      </section>
    </main>
  )
}
