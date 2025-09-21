import { configureHttpClient, httpRequest } from '../api/httpClient'
import type { AuthSession, LoginCredentials } from '../types/auth'

const SESSION_STORAGE_KEY = 'avacare.sales.session'

let session: AuthSession | null = null

configureHttpClient(() => session?.token ?? null)

const persistSession = (value: AuthSession | null) => {
  session = value
  if (typeof window === 'undefined') {
    return
  }

  if (value) {
    window.sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(value))
  } else {
    window.sessionStorage.removeItem(SESSION_STORAGE_KEY)
  }
}

const readSessionFromStorage = (): AuthSession | null => {
  if (typeof window === 'undefined') {
    return session
  }

  const stored = window.sessionStorage.getItem(SESSION_STORAGE_KEY)
  if (!stored) {
    return null
  }

  try {
    return JSON.parse(stored) as AuthSession
  } catch {
    window.sessionStorage.removeItem(SESSION_STORAGE_KEY)
    return null
  }
}

export const authService = {
  async login(credentials: LoginCredentials): Promise<AuthSession> {
    const result = await httpRequest<AuthSession>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
      requiresAuth: false,
    })

    persistSession(result)
    return result
  },
  logout() {
    persistSession(null)
  },
  getSession(): AuthSession | null {
    if (session) {
      return session
    }

    const stored = readSessionFromStorage()
    if (stored) {
      session = stored
    }

    return session
  },
}
