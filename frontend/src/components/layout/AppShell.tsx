import { useEffect, useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../../lib/auth/AuthProvider'
import { Sheet, SheetContent, SheetTitle } from '../ui/sheet'
import { TooltipProvider } from '../ui/tooltip'
import { CommandPalette } from './CommandPalette'
import { Sidebar } from './Sidebar'
import { Topbar } from './Topbar'
import { findNavigationItem } from './navigation'

const collapsedStorageKey = 'swm.ui.sidebarCollapsed'

export function AppShell() {
  const { user, logout } = useAuth()
  const location = useLocation()
  const [collapsed, setCollapsed] = useState(() => window.localStorage.getItem(collapsedStorageKey) === 'true')
  const [mobileOpen, setMobileOpen] = useState(false)
  const [commandOpen, setCommandOpen] = useState(false)
  const currentItem = findNavigationItem(location.pathname)

  const openCommandPalette = () => {
    window.setTimeout(() => setCommandOpen(true), 0)
  }

  useEffect(() => {
    window.localStorage.setItem(collapsedStorageKey, String(collapsed))
  }, [collapsed])

  if (!user) {
    return null
  }

  return (
    <TooltipProvider delayDuration={250}>
      <div className="min-h-screen bg-page text-text-primary">
        <div className="hidden fixed inset-y-0 left-0 z-sticky md:block">
          <Sidebar user={user} collapsed={collapsed} onCollapsedChange={setCollapsed} />
        </div>
        <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
          <SheetContent className="left-0 right-auto w-72 border-l-0 border-r border-white/10 bg-sidebar p-0">
            <SheetTitle className="sr-only">Navigation</SheetTitle>
            <Sidebar user={user} collapsed={false} onCollapsedChange={() => undefined} onNavigate={() => setMobileOpen(false)} showCollapseControl={false} />
          </SheetContent>
        </Sheet>
        <div className={collapsed ? 'md:pl-20' : 'md:pl-72'}>
          <Topbar user={user} currentItem={currentItem} onMobileMenu={() => setMobileOpen(true)} onCommandPalette={openCommandPalette} onLogout={() => void logout()} />
          <main className="min-h-[calc(100vh-4rem)] px-4 py-5 md:px-6 lg:px-8">
            <Outlet />
          </main>
        </div>
        <CommandPalette open={commandOpen} onOpenChange={setCommandOpen} roles={user.roles} />
      </div>
    </TooltipProvider>
  )
}
