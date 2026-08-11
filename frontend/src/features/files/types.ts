export type FileStatus = 'UPLOAD_PENDING' | 'UPLOADED' | 'CONFIRMED' | 'DELETED'

export type PaginatedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export type DepartmentFile = {
  id: string
  folderId: string | null
  uploadedById: string
  fileName: string
  storageKey: string
  fileSize: number
  mimeType: string
  fileExtension: string
  contentHash: string | null
  status: FileStatus
  confirmedAt: string | null
  createdAt: string
}

export type FileFolder = {
  id: string
  parentFolderId: string | null
  name: string
  createdAt: string
}

export type DepartmentFileUploadIntent = {
  fileId: string
  storageKey: string
  fileName: string
  fileSize: number
  mimeType: string
  fileExtension: string
  status: FileStatus
  signedUploadUrl: string
  uploadMethod: string
  requiredHeaders: Record<string, string>
  expiresAt: string
}

export type DepartmentFileDownload = {
  fileId: string
  storageKey: string
  downloadUrl: string
  expiresAt: string
}

export type FileListFilters = {
  page?: number
  pageSize?: number
  search?: string
  folderId?: string | null
}
