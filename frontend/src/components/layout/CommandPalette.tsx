import { Command } from 'cmdk'
import { Search } from 'lucide-react'
import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import type { UserRole } from '../../lib/auth/authTypes'
import { Dialog, DialogContent, DialogTitle } from '../ui/dialog'
import { visibleNavigationGroups } from './navigation'

export function CommandPalette({ open, onOpenChange, roles }: { open: boolean; onOpenChange: (open: boolean) => void; roles: UserRole[] }) {
  const navigate = useNavigate()
  const groups = visibleNavigationGroups(roles)

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault()
        onOpenChange(!open)
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [onOpenChange, open])

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="z-commandPalette max-w-xl p-0">
        <DialogTitle className="sr-only">Command Palette</DialogTitle>
        <Command label="Command Palette" className="overflow-hidden rounded-lg bg-surface">
          <div className="flex items-center gap-2 border-b border-border px-4">
            <Search aria-hidden="true" className="h-4 w-4 text-text-muted" />
            <Command.Input placeholder="Navigate to..." className="h-12 flex-1 bg-transparent text-sm outline-none placeholder:text-text-muted" />
          </div>
          <Command.List className="max-h-80 overflow-y-auto p-2">
            <Command.Empty className="px-3 py-8 text-center text-sm text-text-secondary">No navigation results found.</Command.Empty>
            {groups.map((group) => (
              <Command.Group key={group.label} heading={group.label} className="[&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:py-2 [&_[cmdk-group-heading]]:text-xs [&_[cmdk-group-heading]]:font-semibold [&_[cmdk-group-heading]]:text-text-muted">
                {group.items.map((item) => {
                  const Icon = item.icon
                  return (
                    <Command.Item
                      key={item.href}
                      value={`${group.label} ${item.label}`}
                      className="flex cursor-default items-center gap-2 rounded-md px-2.5 py-2 text-sm text-text-primary outline-none aria-selected:bg-surface-secondary"
                      onSelect={() => {
                        navigate(item.href)
                        onOpenChange(false)
                      }}
                    >
                      <Icon aria-hidden="true" className="h-4 w-4 text-text-muted" />
                      {item.label}
                    </Command.Item>
                  )
                })}
              </Command.Group>
            ))}
          </Command.List>
        </Command>
      </DialogContent>
    </Dialog>
  )
}
