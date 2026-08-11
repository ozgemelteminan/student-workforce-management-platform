import { buildApiUrl } from './api'

export type SignedUploadTarget = {
  signedUploadUrl: string
  uploadMethod?: string
  requiredHeaders?: Record<string, string>
}

export type SignedUploadOptions = {
  signal?: AbortSignal
  onProgress?: (progress: number) => void
}

export type SignedDownloadTarget = {
  downloadUrl: string
  fileName?: string
}

export function resolveSignedUrl(url: string): string {
  return new URL(url, buildApiUrl('/')).toString()
}

export function uploadSignedFile(target: SignedUploadTarget, file: File, options: SignedUploadOptions = {}) {
  return new Promise<void>((resolve, reject) => {
    const request = new XMLHttpRequest()
    request.open(target.uploadMethod || 'PUT', resolveSignedUrl(target.signedUploadUrl))
    Object.entries(target.requiredHeaders ?? {}).forEach(([key, value]) => request.setRequestHeader(key, value))
    request.upload.onprogress = (event) => {
      if (event.lengthComputable) {
        options.onProgress?.(Math.round((event.loaded / event.total) * 100))
      }
    }
    request.onload = () => {
      if (request.status >= 200 && request.status < 300) {
        resolve()
      } else {
        reject(new Error('Signed upload failed.'))
      }
    }
    request.onerror = () => reject(new Error('Signed upload failed.'))
    request.onabort = () => reject(new DOMException('Upload cancelled.', 'AbortError'))
    options.signal?.addEventListener('abort', () => request.abort(), { once: true })
    request.send(file)
  })
}

export function openSignedDownload(target: SignedDownloadTarget): void {
  const anchor = document.createElement('a')
  anchor.href = resolveSignedUrl(target.downloadUrl)
  anchor.rel = 'noopener noreferrer'
  anchor.target = '_blank'
  if (target.fileName) {
    anchor.download = target.fileName
  }
  document.body.append(anchor)
  anchor.click()
  anchor.remove()
}
