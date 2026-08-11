export const MAX_UPLOAD_BYTES = 1_073_741_824

export const ALLOWED_UPLOAD_MIME_TYPES_BY_EXTENSION: Record<string, readonly string[]> = {
  '.doc': ['application/msword'],
  '.docx': ['application/vnd.openxmlformats-officedocument.wordprocessingml.document'],
  '.xls': ['application/vnd.ms-excel'],
  '.xlsx': ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'],
  '.ppt': ['application/vnd.ms-powerpoint'],
  '.pptx': ['application/vnd.openxmlformats-officedocument.presentationml.presentation'],
  '.odt': ['application/vnd.oasis.opendocument.text'],
  '.ods': ['application/vnd.oasis.opendocument.spreadsheet'],
  '.odp': ['application/vnd.oasis.opendocument.presentation'],
  '.pdf': ['application/pdf'],
  '.txt': ['text/plain'],
  '.csv': ['text/csv', 'application/csv'],
  '.json': ['application/json', 'text/json'],
  '.md': ['text/markdown', 'text/plain'],
  '.png': ['image/png'],
  '.jpg': ['image/jpeg'],
  '.jpeg': ['image/jpeg'],
  '.webp': ['image/webp'],
  '.zip': ['application/zip', 'application/x-zip-compressed', 'multipart/x-zip'],
}

export function formatFileSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) {
    return '0 B'
  }

  const units = ['B', 'KB', 'MB', 'GB']
  let value = bytes
  let unitIndex = 0
  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024
    unitIndex += 1
  }

  const formatted = unitIndex === 0 || value >= 10 ? Math.round(value).toString() : value.toFixed(1)
  return `${formatted} ${units[unitIndex]}`
}

export function getFileExtension(fileName: string): string {
  const index = fileName.lastIndexOf('.')
  return index > -1 ? fileName.slice(index).toLowerCase() : ''
}

export function validateUploadFile(file: File): string | null {
  if (file.size <= 0) {
    return 'File must not be empty.'
  }
  if (file.size > MAX_UPLOAD_BYTES) {
    return 'File must be 1 GB or smaller.'
  }
  const extension = getFileExtension(file.name)
  const allowedMimeTypes = ALLOWED_UPLOAD_MIME_TYPES_BY_EXTENSION[extension]
  if (!allowedMimeTypes) {
    return 'This file type is not allowed.'
  }
  const mimeType = normalizeMimeType(file.type)
  if (mimeType && !allowedMimeTypes.includes(mimeType)) {
    return 'The file type does not match the selected file extension.'
  }
  return null
}

function normalizeMimeType(value: string): string {
  return value.trim().toLowerCase().split(';')[0]?.trim() ?? ''
}
