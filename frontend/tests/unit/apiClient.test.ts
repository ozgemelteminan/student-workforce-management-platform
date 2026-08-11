import { apiRequest, configureApiAuth } from '../../src/lib/api'

describe('apiRequest', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('coordinates simultaneous 401 responses through one refresh attempt and retries once', async () => {
    let accessToken = 'old-token'
    const refresh = vi.fn(async () => {
      await Promise.resolve()
      accessToken = 'new-token'
      return true
    })

    configureApiAuth({
      getAccessToken: () => accessToken,
      refresh,
      clear: vi.fn(),
    })

    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValueOnce(new Response(JSON.stringify({ title: 'Unauthorized' }), { status: 401, headers: { 'content-type': 'application/json' } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ title: 'Unauthorized' }), { status: 401, headers: { 'content-type': 'application/json' } }))
      .mockImplementation(async () => new Response(JSON.stringify({ ok: true }), { status: 200, headers: { 'content-type': 'application/json' } }))

    vi.stubGlobal('fetch', fetchMock)

    await Promise.all([apiRequest('/skills'), apiRequest('/categories')])

    expect(refresh).toHaveBeenCalledTimes(1)
    expect(fetchMock).toHaveBeenCalledTimes(4)
    const retriedRequest = fetchMock.mock.calls[2]?.[0]
    expect(retriedRequest).toBeInstanceOf(Request)
    expect((retriedRequest as Request).headers.get('Authorization')).toBe('Bearer new-token')
  })
})
