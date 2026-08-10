import { useTranslation } from 'react-i18next'

export function FoundationPage() {
  const { t } = useTranslation()

  return (
    <main className="min-h-screen px-6 py-8">
      <section className="mx-auto max-w-5xl">
        <p className="text-sm font-medium uppercase tracking-wide text-sky-700">/api/v1</p>
        <h1 className="mt-3 text-3xl font-semibold text-slate-950">{t('app.foundationTitle')}</h1>
        <p className="mt-4 max-w-2xl text-base leading-7 text-slate-700">{t('app.foundationStatus')}</p>
      </section>
    </main>
  )
}
