import { afterEach, describe, expect, it, vi } from 'vitest'
import { getSubmissionVersionDownloadUrl, getTasks, initiateSubmissionUpload } from './tasksApi'

describe('tasks api client contracts', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('serializes unassigned filtering for server-side task queries', async () => {
    const fetchMock = vi.fn(async (_input: RequestInfo | URL, _init?: RequestInit) => Response.json({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false }))
    vi.stubGlobal('fetch', fetchMock)

    await getTasks({ page: 1, pageSize: 20, isAssigned: false })

    const request = fetchMock.mock.calls[0]?.[0]
    if (!(request instanceof Request)) {
      throw new Error('Expected getTasks to issue a Request')
    }
    expect(request.url).toContain('/api/v1/tasks?')
    expect(request.url).toContain('isAssigned=false')
  })

  it('requests signed upload and download URLs through authenticated API endpoints only', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, _init?: RequestInit) => {
      const request = input as Request
      if (request.url.includes('/uploads/initiate')) {
        return Response.json({ submissionVersionId: 'version-id', taskSubmissionId: 'submission-id', versionNumber: 1, storageKey: 'task-submissions/key.txt', fileName: 'file.txt', fileSize: 4, mimeType: 'text/plain', fileExtension: '.txt', fileStatus: 'UPLOAD_PENDING', signedUploadUrl: '/api/v1/storage/local/uploads/id?token=redacted', uploadMethod: 'PUT', requiredHeaders: { 'Content-Type': 'text/plain' }, expiresAt: '2026-08-11T12:00:00Z' })
      }
      return Response.json({ submissionVersionId: 'version-id', fileName: 'file.txt', fileSize: 4, signedDownloadUrl: '/api/v1/storage/local/downloads?token=redacted', expiresAt: '2026-08-11T12:00:00Z' })
    })
    vi.stubGlobal('fetch', fetchMock)

    const upload = await initiateSubmissionUpload('task-id', new File(['test'], 'file.txt', { type: 'text/plain' }))
    const download = await getSubmissionVersionDownloadUrl('submission-id', 'version-id')

    expect(upload.signedUploadUrl).toContain('/storage/local/uploads')
    expect(upload.requiredHeaders['Content-Type']).toBe('text/plain')
    expect(download.signedDownloadUrl).toContain('/storage/local/downloads')
    expect(fetchMock).toHaveBeenCalledTimes(2)
    const uploadRequest = fetchMock.mock.calls[0]?.[0]
    const downloadRequest = fetchMock.mock.calls[1]?.[0]
    if (!(uploadRequest instanceof Request) || !(downloadRequest instanceof Request)) {
      throw new Error('Expected submission API calls to issue Requests')
    }
    expect(uploadRequest.url).toContain('/tasks/task-id/uploads/initiate')
    expect(downloadRequest.url).toContain('/submissions/submission-id/versions/version-id/download-url')
  })
})
