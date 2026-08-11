import { Menu, Search } from 'lucide-react'
import type { AuthUser } from '../../lib/auth/authTypes'
import { Button } from '../ui/button'
import { IconButton } from '../ui/icon-button'
import { Breadcrumb } from '../ui/breadcrumb'
import { NotificationBell } from './NotificationBell'
import { UserMenu } from './UserMenu'
import type { NavigationItem } from './navigation'

export function Topbar({
  user,
  currentItem,
  onMobileMenu,
  onCommandPalette,
  onLogout,
}: {
  user: AuthUser
  currentItem?: NavigationItem
  onMobileMenu: () => void
  onCommandPalette: () => void
  onLogout: () => void
}) {
  return (
    <header className="sticky top-0 z-sticky flex h-16 items-center justify-between border-b border-border bg-page/95 px-4 backdrop-blur md:px-6">
      <div className="flex min-w-0 items-center gap-3">
        <IconButton label="Open navigation" icon={<Menu aria-hidden="true" className="h-4 w-4" />} className="md:hidden" onClick={onMobileMenu} />
        <Breadcrumb items={[{ label: 'Workspace', href: '/' }, { label: currentItem?.label ?? 'Workspace' }]} />
      </div>
      <div className="flex items-center gap-2">
        <Button variant="outline" className="hidden w-56 justify-start text-text-secondary sm:flex" iconBefore={<Search aria-hidden="true" className="h-4 w-4" />} onClick={onCommandPalette}>
          Search or jump...
          <kbd className="ml-auto rounded border border-border bg-surface-secondary px-1.5 py-0.5 text-[0.65rem] text-text-muted">⌘K</kbd>
        </Button>
        <IconButton label="Open command palette" tooltip="Command Palette" icon={<Search aria-hidden="true" className="h-4 w-4" />} className="sm:hidden" onClick={onCommandPalette} />
        <NotificationBell />
        <UserMenu user={user} onLogout={onLogout} />
      </div>
    </header>
  )
}
