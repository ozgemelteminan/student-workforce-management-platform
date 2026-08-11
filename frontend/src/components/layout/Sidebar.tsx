import { PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import type { AuthUser } from '../../lib/auth/authTypes'
import { cn } from '../../lib/utils/cn'
import { IconButton } from '../ui/icon-button'
import { Tooltip, TooltipContent, TooltipTrigger } from '../ui/tooltip'
import { visibleNavigationGroups } from './navigation'

export type SidebarProps = {
  user: AuthUser
  collapsed: boolean
  onCollapsedChange: (collapsed: boolean) => void
  onNavigate?: () => void
  showCollapseControl?: boolean
}

export function Sidebar({ user, collapsed, onCollapsedChange, onNavigate, showCollapseControl = true }: SidebarProps) {
  const groups = visibleNavigationGroups(user.roles)
  const primaryRole = user.roles[0] ?? 'STUDENT'

  return (
    <aside className={cn('flex h-full flex-col bg-sidebar text-text-inverse transition-[width] duration-200 motion-reduce:transition-none', collapsed ? 'w-20' : 'w-72')}>
      <div className="flex h-16 items-center justify-between border-b border-white/10 px-4">
        <div className={cn('min-w-0', collapsed && 'sr-only')}>
          <p className="truncate text-sm font-semibold">Student Workforce</p>
          <p className="truncate text-xs text-white/55">Management Platform</p>
        </div>
        {showCollapseControl ? (
          <IconButton
            label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            tooltip={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            icon={collapsed ? <PanelLeftOpen aria-hidden="true" className="h-4 w-4" /> : <PanelLeftClose aria-hidden="true" className="h-4 w-4" />}
            className="text-white/70 hover:bg-sidebar-elevated hover:text-white"
            onClick={() => onCollapsedChange(!collapsed)}
          />
        ) : null}
      </div>

      <nav aria-label="Primary navigation" className="min-h-0 flex-1 overflow-y-auto px-3 py-4">
        <div className="space-y-5">
          {groups.map((group) => (
            <div key={group.label}>
              <p className={cn('mb-2 px-2 text-[0.68rem] font-semibold uppercase tracking-wider text-white/40', collapsed && 'sr-only')}>{group.label}</p>
              <div className="space-y-1">
                {group.items.map((item) => {
                  const Icon = item.icon
                  const link = (
                    <NavLink
                      to={item.href}
                      end={item.href === '/'}
                      onClick={onNavigate}
                      className={({ isActive }) =>
                        cn(
                          'relative flex h-9 items-center gap-3 rounded-md px-2 text-sm font-medium text-white/65 transition-colors hover:bg-sidebar-elevated hover:text-white focus-visible:outline-brand',
                          collapsed && 'justify-center px-0',
                          isActive && 'bg-sidebar-elevated text-white before:absolute before:left-0 before:h-5 before:w-0.5 before:rounded-r before:bg-brand',
                        )
                      }
                    >
                      <Icon aria-hidden="true" className="h-4 w-4 shrink-0" />
                      <span className={cn('truncate', collapsed && 'sr-only')}>{item.label}</span>
                    </NavLink>
                  )

                  return collapsed ? (
                    <Tooltip key={item.href}>
                      <TooltipTrigger asChild>{link}</TooltipTrigger>
                      <TooltipContent side="right">{item.label}</TooltipContent>
                    </Tooltip>
                  ) : (
                    <div key={item.href}>{link}</div>
                  )
                })}
              </div>
            </div>
          ))}
        </div>
      </nav>

      <div className="border-t border-white/10 p-3">
        <div className={cn('rounded-lg bg-sidebar-elevated p-3', collapsed && 'flex justify-center p-2')} title={collapsed ? `${user.displayName} · ${primaryRole}` : undefined}>
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-brand text-sm font-semibold text-white">
            {(user.displayName || user.email).slice(0, 1).toUpperCase()}
          </div>
          <div className={cn('mt-2 min-w-0', collapsed && 'sr-only')}>
            <p className="truncate text-sm font-medium text-white">{user.displayName || user.email}</p>
            <p className="truncate text-xs text-white/55">{primaryRole}</p>
          </div>
        </div>
      </div>
    </aside>
  )
}
