import { Bell } from 'lucide-react'
import { EmptyState } from '../ui/empty-state'
import { IconButton } from '../ui/icon-button'
import { Popover, PopoverContent, PopoverTrigger } from '../ui/popover'

export function NotificationBell() {
  return (
    <Popover>
      <PopoverTrigger asChild>
        <IconButton label="Open notifications" tooltip="Notifications" icon={<Bell aria-hidden="true" className="h-4 w-4" />} />
      </PopoverTrigger>
      <PopoverContent align="end" className="w-80 p-0">
        <div className="border-b border-border px-4 py-3">
          <h2 className="text-sm font-semibold text-text-primary">Notifications</h2>
        </div>
        <div className="p-3">
          <EmptyState title="No notifications yet." description="Notification integration will be implemented in a later phase." className="min-h-36 border-0 bg-surface-secondary" />
        </div>
      </PopoverContent>
    </Popover>
  )
}
