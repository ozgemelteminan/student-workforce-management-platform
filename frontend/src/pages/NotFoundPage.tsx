import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-page px-6 py-10 text-text-primary">
      <section className="w-full max-w-md rounded-lg border border-border bg-surface p-6">
        <p className="text-sm font-medium text-text-muted">404</p>
        <h1 className="mt-2 text-xl font-semibold">Page not found</h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary">This route is not part of the current frontend phase.</p>
        <Link className="mt-5 inline-flex text-sm font-medium text-brand hover:text-brand-hover" to="/">
          Return to foundation
        </Link>
      </section>
    </main>
  )
}

