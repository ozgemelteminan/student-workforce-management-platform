import { afterEach, describe, expect, it, vi } from 'vitest'
import { getNotificationPreferences, updateNotificationPreference } from './notificationsApi'

describe('notifications api client contracts', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('reads preferences through GET and saves one explicit setting through PUT', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const request = input as Request
      if (request.method === 'GET') {
        return Response.json([{ preferenceType: 'TaskAssigned', channel: 'EMAIL', isEnabled: false }])
      }

      expect(request.method).toBe('PUT')
      expect(await request.json()).toEqual({ preferenceType: 'TaskAssigned', channel: 'EMAIL', isEnabled: true })
      return Response.json({ id: 'preference-id', userId: 'user-id', preferenceType: 'TaskAssigned', channel: 'EMAIL', isEnabled: true })
    })
    vi.stubGlobal('fetch', fetchMock)

    const preferences = await getNotificationPreferences()
    const saved = await updateNotificationPreference({ preferenceType: 'TaskAssigned', channel: 'EMAIL', isEnabled: true })

    expect(preferences[0]).toEqual({ preferenceType: 'TaskAssigned', channel: 'EMAIL', isEnabled: false })
    expect(saved.isEnabled).toBe(true)
    expect(fetchMock).toHaveBeenCalledTimes(2)
    const getRequest = fetchMock.mock.calls[0]?.[0]
    const putRequest = fetchMock.mock.calls[1]?.[0]
    if (!(getRequest instanceof Request) || !(putRequest instanceof Request)) {
      throw new Error('Expected notification API calls to issue Requests')
    }
    expect(getRequest.url).toContain('/api/v1/notifications/preferences')
    expect(putRequest.url).toContain('/api/v1/notifications/preferences')
  })
})
