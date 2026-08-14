import { Bell, Pencil, RefreshCw } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { Button, Card, CardContent, Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, EmptyState, ErrorState, Input, PageHeader, Skeleton, Textarea } from '../../components/ui'
import { useSettingMutations, useSettings } from '../../features/settings/useSettingsQueries'
import type { SystemSetting } from '../../features/settings/types'

const schema = z.object({ value: z.string().max(4000) })
type FormValues = z.infer<typeof schema>

export function SettingsPage() {
  const settings = useSettings()
  const mutations = useSettingMutations()
  const [editing, setEditing] = useState<SystemSetting | null>(null)

  return (
    <div className="space-y-5">
      <PageHeader title="Settings" description="Administrative key/value settings exposed by the API." secondaryActions={<Button variant="outline" iconBefore={<Bell aria-hidden="true" className="h-4 w-4" />} onClick={() => window.location.assign('/notifications')}>Notification preferences</Button>} />
      <Card>
        <CardContent className="space-y-4">
          {settings.isError ? <ErrorState title="Settings could not be loaded." description="Refresh or check administrator access." retryAction={<Button variant="outline" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={() => void settings.refetch()}>Retry</Button>} /> : null}
          {settings.isLoading ? <div className="space-y-2"><Skeleton className="h-16" /><Skeleton className="h-16" /></div> : null}
          {!settings.isLoading && !settings.isError && !settings.data?.length ? <EmptyState title="No system settings are exposed." className="min-h-48" /> : null}
          <div className="divide-y divide-border overflow-hidden rounded-lg border border-border">
            {settings.data?.map((setting) => (
              <div key={setting.id} className="grid gap-3 bg-surface px-4 py-3 lg:grid-cols-[1fr_auto] lg:items-center">
                <div className="min-w-0">
                  <p className="font-medium text-text-primary">{setting.key}</p>
                  {setting.description ? <p className="text-sm text-text-secondary">{setting.description}</p> : null}
                  <p className="mt-1 truncate text-xs text-text-muted">Value: {setting.value}</p>
                </div>
                <Button variant="outline" size="sm" iconBefore={<Pencil aria-hidden="true" className="h-4 w-4" />} onClick={() => setEditing(setting)}>Edit</Button>
              </div>
            ))}
          </div>
          <p className="text-sm text-text-secondary"><Link className="font-medium text-brand hover:text-brand-hover" to="/notifications">Notification preferences</Link> are managed in the Notifications workspace.</p>
        </CardContent>
      </Card>
      <SettingDialog setting={editing} pending={mutations.update.isPending} onClose={() => setEditing(null)} onSubmit={async (value) => { if (editing) await mutations.update.mutateAsync({ key: editing.key, value, concurrencyToken: editing.concurrencyToken }); setEditing(null) }} />
    </div>
  )
}

function SettingDialog({ setting, pending, onClose, onSubmit }: { setting: SystemSetting | null; pending: boolean; onClose: () => void; onSubmit: (value: string) => Promise<void> }) {
  const form = useForm<FormValues>({ resolver: zodResolver(schema), values: { value: setting?.value ?? '' } })
  return (
    <Dialog open={Boolean(setting)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader><DialogTitle>Edit setting</DialogTitle><DialogDescription>Settings are edited as API-provided string values.</DialogDescription></DialogHeader>
        <form className="space-y-4" onSubmit={form.handleSubmit((values) => onSubmit(values.value))}>
          <label className="space-y-1.5 text-sm font-medium text-text-primary">Key<Input value={setting?.key ?? ''} readOnly /></label>
          <label className="space-y-1.5 text-sm font-medium text-text-primary">Value<Textarea rows={6} {...form.register('value')} invalid={Boolean(form.formState.errors.value)} /></label>
          {form.formState.errors.value ? <p className="text-xs text-destructive">{form.formState.errors.value.message}</p> : null}
          <DialogFooter><Button variant="outline" onClick={onClose}>Cancel</Button><Button type="submit" isLoading={pending}>Save</Button></DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
