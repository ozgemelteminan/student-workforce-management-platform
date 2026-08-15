import { afterEach, describe, expect, it, vi } from 'vitest'
import { openSignedDownload, resolveSignedUrl, uploadSignedFile } from './signed-urls'

class FakeXhr {
  static latest: FakeXhr | null = null
  upload = { onprogress: null as ((event: ProgressEvent) => void) | null }
  status = 204
  onload: (() => void) | null = null
  onerror: (() => void) | null = null
  onabort: (() => void) | null = null
  method = ''
  url = ''
  headers: Record<string, string> = {}

  constructor() {
    FakeXhr.latest = this
  }

  open(method: string, url: string) {
    this.method = method
    this.url = url
  }

  setRequestHeader(key: string, value: string) {
    this.headers[key] = value
  }

  send(_file: File) {
    this.upload.onprogress?.({ lengthComputable: true, loaded: 2, total: 4 } as ProgressEvent)
    this.onload?.()
  }

  abort() {
    this.onabort?.()
  }
}

describe('signed url helpers', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    document.body.innerHTML = ''
    FakeXhr.latest = null
  })

  it('resolves relative signed storage URLs against the API base', () => {
    expect(resolveSignedUrl('/api/v1/storage/local/downloads?token=redacted')).toContain('/api/v1/storage/local/downloads?token=redacted')
  })

  it('uploads directly to signed storage URL without bearer authorization', async () => {
    vi.stubGlobal('XMLHttpRequest', FakeXhr)
    const progress = vi.fn()

    await uploadSignedFile({ signedUploadUrl: '/api/v1/storage/local/uploads/id?token=redacted', uploadMethod: 'PUT', requiredHeaders: { 'Content-Type': 'text/plain' } }, new File(['test'], 'file.txt', { type: 'text/plain' }), { onProgress: progress })

    expect(FakeXhr.latest?.method).toBe('PUT')
    expect(FakeXhr.latest?.headers).toEqual({ 'Content-Type': 'text/plain' })
    expect(FakeXhr.latest?.headers.Authorization).toBeUndefined()
    expect(progress).toHaveBeenCalledWith(50)
  })

  it('rejects signed uploads when storage returns a non-success status', async () => {
    vi.stubGlobal('XMLHttpRequest', class extends FakeXhr {
      constructor() {
        super()
        this.status = 403
      }
    })

    await expect(uploadSignedFile({ signedUploadUrl: 'https://storage.example/upload', uploadMethod: 'PUT' }, new File(['test'], 'file.txt'))).rejects.toThrow('Signed upload failed.')
  })

  it('triggers browser-native download through a temporary anchor', () => {
    const anchor = document.createElement('a')
    const click = vi.spyOn(anchor, 'click').mockImplementation(() => undefined)
    vi.spyOn(anchor, 'remove').mockImplementation(() => undefined)
    vi.spyOn(document, 'createElement').mockReturnValue(anchor)

    openSignedDownload({ downloadUrl: '/api/v1/storage/local/downloads?token=redacted', fileName: 'file.txt' })

    expect(click).toHaveBeenCalledTimes(1)
  })
})
