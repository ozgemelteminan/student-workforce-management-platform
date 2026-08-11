import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query'
import { appToast } from '../../lib/toast'
import { createAnnouncement, deleteAnnouncement, getAnnouncement, getAnnouncements, pinAnnouncement, publishAnnouncement, unpinAnnouncement, unpublishAnnouncement, updateAnnouncement } from './api/announcementsApi'
import type { AnnouncementFilters, AnnouncementPayload } from './types'

export function useAnnouncements(filters: AnnouncementFilters) {
  return useQuery({
    queryKey: queryKeys.announcements.list(stableFilters(filters)),
    queryFn: ({ signal }) => getAnnouncements(filters, signal),
  })
}

export function useAnnouncement(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.announcements.detail(id ?? 'missing'),
    queryFn: ({ signal }) => getAnnouncement(id ?? '', signal),
    enabled: Boolean(id),
  })
}

export function useAnnouncementMutations(id?: string) {
  const queryClient = useQueryClient()
  const invalidate = async (announcementId = id) => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.announcements.all }),
      announcementId ? queryClient.invalidateQueries({ queryKey: queryKeys.announcements.detail(announcementId) }) : Promise.resolve(),
    ])
  }

  return {
    create: useMutation({
      mutationFn: (payload: AnnouncementPayload) => createAnnouncement(payload),
      onSuccess: async (announcement) => {
        appToast.success('Announcement created.')
        await invalidate(announcement.id)
      },
    }),
    update: useMutation({
      mutationFn: ({ id: announcementId, payload }: { id: string; payload: AnnouncementPayload }) => updateAnnouncement(announcementId, payload),
      onSuccess: async (announcement) => {
        appToast.success('Announcement updated.')
        await invalidate(announcement.id)
      },
    }),
    publish: useMutation({ mutationFn: (announcementId: string) => publishAnnouncement(announcementId), onSuccess: async (announcement) => invalidate(announcement.id) }),
    unpublish: useMutation({ mutationFn: (announcementId: string) => unpublishAnnouncement(announcementId), onSuccess: async (announcement) => invalidate(announcement.id) }),
    pin: useMutation({ mutationFn: (announcementId: string) => pinAnnouncement(announcementId), onSuccess: async (announcement) => invalidate(announcement.id) }),
    unpin: useMutation({ mutationFn: (announcementId: string) => unpinAnnouncement(announcementId), onSuccess: async (announcement) => invalidate(announcement.id) }),
    delete: useMutation({
      mutationFn: (announcementId: string) => deleteAnnouncement(announcementId),
      onSuccess: async () => {
        appToast.success('Announcement deleted.')
        await invalidate()
      },
    }),
  }
}

function stableFilters(filters: Record<string, unknown>) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== ''))
}
