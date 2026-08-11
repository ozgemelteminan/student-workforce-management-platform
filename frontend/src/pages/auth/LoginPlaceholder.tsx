import { LogIn } from 'lucide-react'

export function LoginPlaceholder() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-page px-6 py-10 text-text-primary">
      <section className="w-full max-w-sm rounded-lg border border-border bg-surface p-6">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-brand-subtle text-brand">
          <LogIn aria-hidden="true" size={20} />
        </div>
        <h1 className="mt-5 text-xl font-semibold">Sign in</h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary">
          Authentication UI is deferred to the next frontend phase. The session foundation is ready.
        </p>
      </section>
    </main>
  )
}

