import { apiRequest } from '../../../lib/api'
import { openSignedDownload, uploadSignedFile } from '../../../lib/signed-urls'
import { getFileExtension } from '../filePresentation'
import type { DepartmentFile, DepartmentFileDownload, DepartmentFileUploadIntent, FileFolder, FileListFilters, PaginatedResult } from '../types'

function params(filters: Record<string, string | number | null | undefined>) {
  const search = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function getDepartmentFiles(filters: FileListFilters, signal?: AbortSignal) {
  return apiRequest<PaginatedResult<DepartmentFile>>(`/files${params(filters)}`, { signal })
}

export function getFileFolders(parentFolderId: string | null, signal?: AbortSignal) {
  return apiRequest<FileFolder[]>(`/files/folders${params({ parentFolderId })}`, { signal })
}

export function createFileFolder(parentFolderId: string | null, name: string) {
  return apiRequest<FileFolder>('/files/folders', { method: 'POST', body: { parentFolderId, name } })
}

export function renameFileFolder(id: string, name: string) {
  return apiRequest<FileFolder>(`/files/folders/${id}`, { method: 'PUT', body: { name } })
}

export function deleteFileFolder(id: string) {
  return apiRequest<void>(`/files/folders/${id}`, { method: 'DELETE' })
}

export function initiateDepartmentFileUpload(folderId: string | null, file: File, contentHash?: string) {
  return apiRequest<DepartmentFileUploadIntent>('/files/uploads', {
    method: 'POST',
    body: {
      folderId,
      fileName: file.name,
      fileSize: file.size,
      mimeType: file.type || 'application/octet-stream',
      fileExtension: getFileExtension(file.name),
      contentHash,
    },
  })
}

export function completeDepartmentFileUpload(fileId: string) {
  return apiRequest<DepartmentFile>(`/files/${fileId}/complete`, { method: 'POST' })
}

export async function uploadDepartmentFile(folderId: string | null, file: File, options: { signal?: AbortSignal; onProgress?: (progress: number) => void } = {}) {
  const intent = await initiateDepartmentFileUpload(folderId, file)
  await uploadSignedFile(intent, file, options)
  return completeDepartmentFileUpload(intent.fileId)
}

export function getDepartmentFileDownload(fileId: string, signal?: AbortSignal) {
  return apiRequest<DepartmentFileDownload>(`/files/${fileId}/download`, { signal })
}

export async function downloadDepartmentFile(file: DepartmentFile) {
  const target = await getDepartmentFileDownload(file.id)
  openSignedDownload({ downloadUrl: target.downloadUrl, fileName: file.fileName })
}

export function deleteDepartmentFile(id: string) {
  return apiRequest<void>(`/files/${id}`, { method: 'DELETE' })
}
