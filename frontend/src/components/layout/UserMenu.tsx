import { LogOut, UserCircle } from 'lucide-react'
import type { AuthUser } from '../../lib/auth/authTypes'
import { Button } from '../ui/button'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '../ui/dropdown-menu'

export function UserMenu({ user, onLogout }: { user: AuthUser; onLogout: () => void }) {
  const primaryRole = user.roles[0] ?? 'STUDENT'

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" className="max-w-56 justify-start px-2" aria-label="Open user menu">
          <span className="flex h-7 w-7 items-center justify-center rounded-md bg-brand text-xs font-semibold text-white">
            {(user.displayName || user.email).slice(0, 1).toUpperCase()}
          </span>
          <span className="hidden min-w-0 text-left sm:block">
            <span className="block truncate text-sm text-text-primary">{user.displayName || user.email}</span>
            <span className="block truncate text-xs text-text-muted">{primaryRole}</span>
          </span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuLabel>
          <span className="block normal-case tracking-normal text-text-primary">{user.displayName || user.email}</span>
          <span className="block truncate text-xs font-normal normal-case tracking-normal text-text-muted">{user.email}</span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator className="my-1 h-px bg-border" />
        <DropdownMenuItem disabled>
          <UserCircle aria-hidden="true" className="h-4 w-4" />
          Profile
        </DropdownMenuItem>
        <DropdownMenuSeparator className="my-1 h-px bg-border" />
        <DropdownMenuItem onSelect={onLogout} destructive>
          <LogOut aria-hidden="true" className="h-4 w-4" />
          Logout
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
