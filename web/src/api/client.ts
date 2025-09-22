import type {
  ApplyPaymentRequest,
  CreateOrderRequest,
  Customer,
  LoginRequest,
  LoginResult,
  Payment,
  Product,
  SalesOrder,
} from './types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''
const SESSION_STORAGE_KEY = 'avacare.sales.session'

class SessionStore {
  private current: LoginResult | null

  constructor() {
    this.current = this.read()
  }

  get value(): LoginResult | null {
    if (this.current) {
      return this.current
    }

    this.current = this.read()
    return this.current
  }

  set value(session: LoginResult | null) {
    this.current = session
    this.persist(session)
  }

  private read(): LoginResult | null {
    if (typeof window === 'undefined') {
      return null
    }

    try {
      const raw = window.localStorage.getItem(SESSION_STORAGE_KEY)
      if (!raw) {
        return null
      }

      const parsed = JSON.parse(raw) as Partial<LoginResult>
      if (typeof parsed?.token === 'string' && typeof parsed?.username === 'string') {
        return { token: parsed.token, username: parsed.username }
      }
    } catch (error) {
      console.warn('Failed to read stored session', error)
    }

    return null
  }

  private persist(session: LoginResult | null): void {
    if (typeof window === 'undefined') {
      return
    }

    if (!session) {
      window.localStorage.removeItem(SESSION_STORAGE_KEY)
      return
    }

    window.localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
  }
}

export class ApiError extends Error {
  readonly status: number
  readonly details?: unknown

  constructor(message: string, status: number, details?: unknown) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.details = details
    Object.setPrototypeOf(this, ApiError.prototype)
  }
}

const sessionStore = new SessionStore()

interface FetchOptions {
  skipAuth?: boolean
}

async function apiFetch<T>(
  path: string,
  init?: RequestInit,
  { skipAuth = false }: FetchOptions = {},
): Promise<T> {
  const headers = new Headers(init?.headers ?? undefined)
  headers.set('Accept', 'application/json')

  if (init?.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  if (!skipAuth) {
    const token = sessionStore.value?.token
    if (!token) {
      throw new ApiError('Authentication is required.', 401)
    }

    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers,
  })

  const text = await response.text()
  let data: unknown = null

  if (text) {
    try {
      data = JSON.parse(text)
    } catch {
      data = text
    }
  }

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`
    if (data && typeof data === 'object' && 'message' in data) {
      const maybeMessage = (data as Record<string, unknown>).message
      if (typeof maybeMessage === 'string' && maybeMessage.trim().length > 0) {
        message = maybeMessage
      }
    }

    throw new ApiError(message, response.status, data)
  }

  return data as T
}

export function getSession(): LoginResult | null {
  return sessionStore.value
}

export function logout(): void {
  sessionStore.value = null
}

export async function login(request: LoginRequest): Promise<LoginResult> {
  const result = await apiFetch<LoginResult>(
    '/auth/login',
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
    { skipAuth: true },
  )

  sessionStore.value = result
  return result
}

export async function fetchCustomer(customerId: string): Promise<Customer> {
  return apiFetch<Customer>(`/customers/${encodeURIComponent(customerId)}`)
}

export async function fetchProducts(): Promise<ReadonlyArray<Product>> {
  return apiFetch<ReadonlyArray<Product>>('/products')
}

export async function createOrder(request: CreateOrderRequest): Promise<SalesOrder> {
  return apiFetch<SalesOrder>('/orders', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export async function applyPayment(request: ApplyPaymentRequest): Promise<Payment> {
  return apiFetch<Payment>('/payments', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export type {
  ApplyPaymentRequest,
  CreateOrderRequest,
  Customer,
  LoginRequest,
  LoginResult,
  Payment,
  Product,
  SalesOrder,
  SalesOrderLine,
} from './types'
