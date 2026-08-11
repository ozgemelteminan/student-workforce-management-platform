import { QueryClientProvider } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { Toaster } from 'sonner'
import { AuthProvider } from '../../lib/auth/AuthProvider'
import { createAppQueryClient } from '../../lib/query'
import '../../i18n'

type AppProvidersProps = {
  children: ReactNode
}

export function AppProviders({ children }: AppProvidersProps) {
  const [queryClient] = useState(() => createAppQueryClient())

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        {children}
        <Toaster
          closeButton
          position="top-right"
          toastOptions={{
            classNames: {
              toast: 'border-border bg-surface text-text-primary shadow-elevated',
              title: 'text-text-primary',
              description: 'text-text-secondary',
              actionButton: 'bg-brand text-text-inverse',
              cancelButton: 'bg-surface-secondary text-text-primary',
              closeButton: 'bg-surface border-border text-text-secondary',
            },
          }}
        />
      </AuthProvider>
    </QueryClientProvider>
  )
}
