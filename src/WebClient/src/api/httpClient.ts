export interface HttpRequestOptions extends RequestInit {
  requiresAuth?: boolean
}

let tokenProvider: (() => string | null) | null = null

export const configureHttpClient = (provider: () => string | null) => {
  tokenProvider = provider
}

export const httpRequest = async <T>(path: string, options: HttpRequestOptions = {}): Promise<T> => {
  const { requiresAuth = true, headers, body, ...rest } = options
  const requestHeaders = new Headers(headers)

  if (body && !requestHeaders.has('Content-Type')) {
    requestHeaders.set('Content-Type', 'application/json')
  }

  if (requiresAuth && tokenProvider) {
    const token = tokenProvider()
    if (token) {
      requestHeaders.set('Authorization', `Bearer ${token}`)
    }
  }

  const baseUrl = import.meta.env.VITE_API_BASE_URL ?? ''
  const response = await fetch(`${baseUrl}${path}`, {
    ...rest,
    headers: requestHeaders,
    body,
  })

  const contentType = response.headers.get('content-type') ?? ''

  if (!response.ok) {
    let message = response.statusText

    if (contentType.includes('application/json')) {
      try {
        const problem = await response.json()
        message = problem.message ?? problem.error ?? message
      } catch {
        // ignore parse errors
      }
    } else {
      const raw = await response.text()
      if (raw) {
        message = raw
      }
    }

    throw new Error(message || 'Request failed')
  }

  if (response.status === 204) {
    return undefined as T
  }

  if (!contentType) {
    return undefined as T
  }

  if (contentType.includes('application/json')) {
    return (await response.json()) as T
  }

  return (await response.text()) as T
}
