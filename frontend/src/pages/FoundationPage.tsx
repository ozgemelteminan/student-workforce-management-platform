import { useTranslation } from 'react-i18next'
import { ShieldCheck } from 'lucide-react'
import { formatIstanbulDateTime } from '../lib/date-time'

export function FoundationPage() {
  const { t } = useTranslation()

  return (
    <main className="min-h-screen bg-page px-6 py-8 text-text-primary">
      <section className="mx-auto max-w-5xl rounded-lg border border-border bg-surface p-6">
        <div className="flex items-start gap-4">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-brand-subtle text-brand">
            <ShieldCheck aria-hidden="true" size={20} />
          </div>
          <div>
            <p className="text-sm font-medium uppercase text-brand">/api/v1</p>
            <h1 className="mt-2 text-2xl font-semibold">{t('app.foundationTitle')}</h1>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-text-secondary">{t('app.foundationStatus')}</p>
            <p className="mt-4 text-xs text-text-muted">Istanbul time: {formatIstanbulDateTime(new Date())}</p>
          </div>
        </div>
      </section>
    </main>
  )
}
