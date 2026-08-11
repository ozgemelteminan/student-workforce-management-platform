import React from 'react'
import { render, screen } from '@testing-library/react'
import { AppProviders } from '../../src/app/providers/AppProviders'
import { FoundationPage } from '../../src/pages/FoundationPage'

describe('FoundationPage', () => {
  it('renders the foundation app shell', () => {
    render(
      <AppProviders>
        <FoundationPage />
      </AppProviders>,
    )

    expect(screen.getByRole('heading', { level: 1, name: /dashboard/i })).toBeInTheDocument()
    expect(screen.getByText(/phase 2 establishes the shared shell/i)).toBeInTheDocument()
  })
})
