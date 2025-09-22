const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

let authToken: string | null = null

export class ApiError extends Error {
  status: number
  details: unknown

  constructor(message: string, status: number, details: unknown) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.details = details
  }
}

type JsonSerializable = Record<string, unknown> | Array<unknown>

type ApiRequestOptions = Omit<RequestInit, 'body'> & {
  auth?: boolean
  body?: RequestInit['body'] | JsonSerializable | null
}

const isFormData = (value: unknown): value is FormData => value instanceof FormData
const isBlob = (value: unknown): value is Blob => value instanceof Blob
const isArrayBuffer = (value: unknown): value is ArrayBuffer => value instanceof ArrayBuffer
const isTypedArray = (value: unknown): value is ArrayBufferView => ArrayBuffer.isView(value as ArrayBufferView)

const normaliseBody = (body: ApiRequestOptions['body'], headers: Headers): BodyInit | null => {
  if (body === undefined || body === null) {
    return null
  }

  if (isFormData(body) || isBlob(body) || isArrayBuffer(body) || isTypedArray(body)) {
    return body as BodyInit
  }

  if (typeof body === 'string') {
    return body
  }

  if (body instanceof URLSearchParams) {
    return body
  }

  if (!headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  return JSON.stringify(body)
}

export const setAuthToken = (token: string | null) => {
  authToken = token
}

export const getAuthToken = () => authToken

export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const { auth = true, body, headers: customHeaders, ...init } = options
  const headers = new Headers(customHeaders)

  const requestBody = normaliseBody(body, headers)

  if (auth && authToken) {
    headers.set('Authorization', `Bearer ${authToken}`)
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers,
    body: requestBody ?? undefined,
  })

  const raw = await response.text()
  let data: unknown = null

  if (raw) {
    try {
      data = JSON.parse(raw)
    } catch {
      data = raw
    }
  }

  if (!response.ok) {
    const message = typeof data === 'string' ? data : (data as { message?: string })?.message ?? response.statusText
    throw new ApiError(message, response.status, data)
  }

  return data as T
}
