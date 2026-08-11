import React, { createRef } from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { Button, Dialog, DialogContent, DialogTitle, DialogTrigger, EmptyState, ErrorState, Input } from '../../src/components/ui'

describe('shared design system primitives', () => {
  it('forwards refs for critical interactive primitives', () => {
    const buttonRef = createRef<HTMLButtonElement>()
    const inputRef = createRef<HTMLInputElement>()

    render(
      <>
        <Button ref={buttonRef}>Save</Button>
        <Input ref={inputRef} aria-label="Name" />
      </>,
    )

    expect(buttonRef.current).toBeInstanceOf(HTMLButtonElement)
    expect(inputRef.current).toBeInstanceOf(HTMLInputElement)
  })

  it('opens an accessible dialog and closes with Escape', async () => {
    const user = userEvent.setup()

    render(
      <Dialog>
        <DialogTrigger asChild>
          <Button>Open dialog</Button>
        </DialogTrigger>
        <DialogContent>
          <DialogTitle>Review changes</DialogTitle>
        </DialogContent>
      </Dialog>,
    )

    await user.click(screen.getByRole('button', { name: /open dialog/i }))
    expect(screen.getByRole('dialog', { name: /review changes/i })).toBeInTheDocument()

    await user.keyboard('{Escape}')
    expect(screen.queryByRole('dialog', { name: /review changes/i })).not.toBeInTheDocument()
  })

  it('renders empty and error state actions without hiding accessible text', async () => {
    const user = userEvent.setup()
    const onRetry = vi.fn()

    render(
      <MemoryRouter>
        <EmptyState title="Nothing here" description="No records are available." primaryAction={<Button>Create</Button>} />
        <ErrorState title="Could not load" description="Try again." retryAction={<Button onClick={onRetry}>Retry</Button>} />
      </MemoryRouter>,
    )

    expect(screen.getByText('No records are available.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /retry/i }))
    expect(onRetry).toHaveBeenCalledTimes(1)
  })
})
