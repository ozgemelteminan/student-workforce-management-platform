import { Pin, RefreshCw, Trash2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
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
  ErrorState,
  PageHeader,
  Skeleton,
} from '../../components/ui'
import { AnnouncementForm } from './AnnouncementsPage'
import { useAuth } from '../../lib/auth/AuthProvider'
import { formatIstanbulDateTime } from '../../lib/date-time'
import { useAnnouncement, useAnnouncementMutations } from '../../features/announcements/useAnnouncementQueries'
import type { AnnouncementPayload } from '../../features/announcements/types'

export function AnnouncementDetailPage() {
  const { announcementId } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('ADMIN') ?? false
  const query = useAnnouncement(announcementId)
  const mutations = useAnnouncementMutations(announcementId)
  const [editOpen, setEditOpen] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [form, setForm] = useState<AnnouncementPayload>({ title: '', content: '', expiresAt: null, isPinned: false })

  useEffect(() => {
    if (!query.data || editOpen) return
    setForm({ title: query.data.title, content: query.data.content, expiresAt: query.data.expiresAt, isPinned: query.data.isPinned })
  }, [editOpen, query.data])

  if (query.isLoading) {
    return <Skeleton className="h-96" />
  }

  if (query.isError || !query.data) {
    return <ErrorState title="Announcement could not be loaded." description="The announcement may not exist, or you may not have access." retryAction={<Button variant="outline" iconBefore={<RefreshCw aria-hidden="true" className="h-4 w-4" />} onClick={() => void query.refetch()}>Retry</Button>} navigationAction={<Button variant="ghost" onClick={() => navigate('/announcements')}>Back</Button>} />
  }

  const announcement = query.data

  const update = async () => {
    const title = form.title.trim()
    const content = form.content.trim()
    if (!announcementId || !title || !content) return
    await mutations.update.mutateAsync({ id: announcementId, payload: { ...form, title, content } })
    setEditOpen(false)
  }

  const deleteItem = async () => {
    if (!announcementId) return
    await mutations.delete.mutateAsync(announcementId)
    navigate('/announcements')
  }

  return (
    <div className="space-y-5">
      <PageHeader
        title={announcement.title}
        breadcrumbs={[{ label: 'Announcements', href: '/announcements' }, { label: announcement.title }]}
        metadata={<><Badge variant={announcement.isPublished ? 'success' : 'neutral'}>{announcement.isPublished ? 'Published' : 'Draft'}</Badge>{announcement.isPinned ? <Badge variant="brand"><Pin aria-hidden="true" className="h-3.5 w-3.5" />Pinned</Badge> : null}</>}
        secondaryActions={isAdmin ? <Button variant="outline" onClick={() => setEditOpen(true)}>Edit</Button> : null}
        primaryAction={isAdmin ? <ManagementActions isPublished={announcement.isPublished} isPinned={announcement.isPinned} onPublish={() => mutations.publish.mutate(announcement.id)} onUnpublish={() => mutations.unpublish.mutate(announcement.id)} onPin={() => mutations.pin.mutate(announcement.id)} onUnpin={() => mutations.unpin.mutate(announcement.id)} onDelete={() => setConfirmDelete(true)} /> : null}
      />

      <Card>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-3 text-xs text-text-muted">
            <span>Created {formatIstanbulDateTime(announcement.createdAt)}</span>
            {announcement.publishedAt ? <span>Published {formatIstanbulDateTime(announcement.publishedAt)}</span> : null}
            <span>Updated {formatIstanbulDateTime(announcement.updatedAt)}</span>
          </div>
          <div className="whitespace-pre-wrap text-sm leading-6 text-text-primary">{announcement.content}</div>
          <Link className="text-sm font-medium text-brand hover:text-brand-hover" to="/announcements">Back to announcements</Link>
        </CardContent>
      </Card>

      <Dialog open={editOpen} onOpenChange={setEditOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit announcement</DialogTitle>
            <DialogDescription>Update the announcement content and pin state.</DialogDescription>
          </DialogHeader>
          <AnnouncementForm form={form} setForm={setForm} />
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditOpen(false)}>Cancel</Button>
            <Button onClick={() => void update()} isLoading={mutations.update.isPending}>Save</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <AlertDialog open={confirmDelete} onOpenChange={setConfirmDelete}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete announcement?</AlertDialogTitle>
            <AlertDialogDescription>This removes the announcement. It is not an unpublish action.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction asChild><Button variant="destructive" iconBefore={<Trash2 aria-hidden="true" className="h-4 w-4" />} onClick={() => void deleteItem()}>Delete</Button></AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function ManagementActions({ isPublished, isPinned, onPublish, onUnpublish, onPin, onUnpin, onDelete }: { isPublished: boolean; isPinned: boolean; onPublish: () => void; onUnpublish: () => void; onPin: () => void; onUnpin: () => void; onDelete: () => void }) {
  return (
    <div className="flex flex-wrap gap-2">
      <Button variant="outline" onClick={isPublished ? onUnpublish : onPublish}>{isPublished ? 'Unpublish' : 'Publish'}</Button>
      <Button variant="outline" onClick={isPinned ? onUnpin : onPin}>{isPinned ? 'Unpin' : 'Pin'}</Button>
      <Button variant="destructive" iconBefore={<Trash2 aria-hidden="true" className="h-4 w-4" />} onClick={onDelete}>Delete</Button>
    </div>
  )
}
