import { afterEach, describe, expect, it, vi } from 'vitest'
import { getSettings, updateSetting } from './settingsApi'

describe('settings api client contracts', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('edits settings by key using value and concurrency token only', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const request = input as Request
      if (request.method === 'PUT') {
        expect(await request.json()).toEqual({ value: 'enabled', concurrencyToken: 'token-id' })
        return Response.json({ key: 'feature.flag', value: 'enabled', concurrencyToken: 'next-token' })
      }
      return Response.json([])
    })
    vi.stubGlobal('fetch', fetchMock)

    await getSettings()
    await updateSetting('feature.flag', 'enabled', 'token-id')

    const updateRequest = fetchMock.mock.calls[1]?.[0]
    if (!(updateRequest instanceof Request)) {
      throw new Error('Expected setting update to issue a Request')
    }
    expect(updateRequest.url).toContain('/settings/feature.flag')
  })
})
