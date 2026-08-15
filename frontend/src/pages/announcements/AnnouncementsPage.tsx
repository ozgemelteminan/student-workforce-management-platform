import { Megaphone, Pin, Plus, RefreshCw } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Badge,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  EmptyState,
  ErrorState,
  Input,
  PageHeader,
  SearchInput,
  Skeleton,
  Textarea,
} from '../../components/ui'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatIstanbulDateTime } from '../../lib/date-time'
import { useAnnouncements, useAnnouncementMutations } from '../../features/announcements/useAnnouncementQueries'
import type { AnnouncementPayload } from '../../features/announcements/types'

const emptyForm: AnnouncementPayload = { title: '', content: '', expiresAt: null, isPinned: false }

export function AnnouncementsPage() {
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('ADMIN') ?? false
  const [search, setSearch] = useState('')
  const [publishedOnly, setPublishedOnly] = useState(!isAdmin)
  const [createOpen, setCreateOpen] = useState(false)
  const [form, setForm] = useState<AnnouncementPayload>(emptyForm)
  const query = useAnnouncements({ page: 1, pageSize: 30, search, publishedOnly })
  const mutations = useAnnouncementMutations()

  const submit = async () => {
    const title = form.title.trim()
    const content = form.content.trim()
    if (!title || !content) return
    await mutations.create.mutateAsync({ ...form, title, content })
    setForm(emptyForm)
    setCreateOpen(false)
  }

  return (
    <div className="space-y-5">
      <PageHeader
        title="Announcements"
        description="Read department updates and manage published notices."
        primaryAction={isAdmin ? <Button iconBefore={<Plus aria-hidden="true" className="h-4 w-4" />} onClick={() => setCreateOpen(true)}>New announcement</Button> : null}
      />

      <Card>
        <CardContent className="space-y-4">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <SearchInput label="Search announcements" placeholder="Search announcements" value={search} onChange={(event) => setSearch(event.target.value)} onClear={() => setSearch('')} className="md:w-80" />
            {isAdmin ? (
              <div className="flex rounded-md border border-border bg-surface-secondary p-1">
                <Button size="sm" variant={publishedOnly ? 'primary' : 'ghost'} onClick={() => setPublishedOnly(true)}>Published</Button>
                <Button size="sm" variant={!publishedOnly ? 'primary' : 'ghost'} onClick={() => setPublishedOnly(false)}>All</Button>
              </div>
            ) : null}
          </div>

          {query.isError ? (
            <ErrorState title="Announcements could not be loaded." description="Refresh the feed or try again after checking your access." retryAction={<Button variant="outline" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={() => void query.refetch()}>Retry</Button>} />
          ) : query.isLoading ? (
            <div className="space-y-3">{Array.from({ length: 4 }).map((_, index) => <Skeleton key={index} className="h-28" />)}</div>
          ) : query.data?.items.length ? (
            <div className="grid gap-3">
              {query.data.items.map((announcement) => (
                <article key={announcement.id} className="rounded-lg border border-border bg-surface px-4 py-4">
                  <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                    <div className="min-w-0 space-y-2">
                      <div className="flex flex-wrap items-center gap-2">
                        {announcement.isPinned ? <Badge variant="brand"><Pin aria-hidden="true" className="h-3.5 w-3.5" />Pinned</Badge> : null}
                        <Badge variant={announcement.isPublished ? 'success' : 'neutral'}>{announcement.isPublished ? 'Published' : 'Draft'}</Badge>
                      </div>
                      <h2 className="text-base font-semibold text-text-primary">
                        <Link className="rounded-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand" to={`/announcements/${announcement.id}`}>{announcement.title}</Link>
                      </h2>
                      <p className="line-clamp-2 whitespace-pre-wrap text-sm text-text-secondary">{announcement.content}</p>
                      <p className="text-xs text-text-muted">{announcement.publishedAt ? `Published ${formatIstanbulDateTime(announcement.publishedAt)}` : `Created ${formatIstanbulDateTime(announcement.createdAt)}`}</p>
                    </div>
                    <Megaphone aria-hidden="true" className="hidden h-5 w-5 shrink-0 text-brand md:block" />
                  </div>
                </article>
              ))}
            </div>
          ) : (
            <EmptyState title={search.trim() ? 'No matching announcements.' : 'No announcements yet.'} description={search.trim() ? 'Clear the search to return to all visible announcements.' : isAdmin ? 'Create an announcement when there is an update to publish.' : 'Published department updates will appear here.'} className="min-h-48" />
          )}
        </CardContent>
      </Card>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>New announcement</DialogTitle>
            <DialogDescription>Create a draft announcement. Publishing is a separate action.</DialogDescription>
          </DialogHeader>
          <AnnouncementForm form={form} setForm={setForm} />
          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)}>Cancel</Button>
            <Button onClick={() => void submit()} isLoading={mutations.create.isPending}>Create</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

export function AnnouncementForm({ form, setForm }: { form: AnnouncementPayload; setForm: (form: AnnouncementPayload) => void }) {
  return (
    <div className="space-y-4">
      <label className="space-y-1.5 text-sm font-medium text-text-primary">
        Title
        <Input maxLength={200} value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} />
      </label>
      <label className="space-y-1.5 text-sm font-medium text-text-primary">
        Content
        <Textarea maxLength={12000} rows={8} value={form.content} onChange={(event) => setForm({ ...form, content: event.target.value })} />
      </label>
      <label className="flex items-center gap-2 text-sm text-text-secondary">
        <input type="checkbox" checked={form.isPinned} onChange={(event) => setForm({ ...form, isPinned: event.target.checked })} />
        Pin this announcement
      </label>
    </div>
  )
}
