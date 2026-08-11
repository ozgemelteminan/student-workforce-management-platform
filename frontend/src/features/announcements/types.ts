export type PaginatedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export type Announcement = {
  id: string
  title: string
  content: string
  createdById: string
  expiresAt: string | null
  isPinned: boolean
  isPublished: boolean
  publishedAt: string | null
  createdAt: string
  updatedAt: string
}

export type AnnouncementFilters = {
  page?: number
  pageSize?: number
  search?: string
  publishedOnly?: boolean
}

export type AnnouncementPayload = {
  title: string
  content: string
  expiresAt: string | null
  isPinned: boolean
}
