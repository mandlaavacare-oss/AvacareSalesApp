import {
  ReactNode,
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react'
import { login as authenticate, LoginRequest, AuthenticatedUser } from '../api/auth'
import { ApiError, setAuthToken } from '../api/client'

const TOKEN_STORAGE_KEY = 'avacare.authToken'
const USER_STORAGE_KEY = 'avacare.user'

interface AuthContextValue {
  token: string | null
  user: AuthenticatedUser | null
  isAuthenticating: boolean
  login: (credentials: LoginRequest) => Promise<void>
  logout: () => void
  lastError: ApiError | null
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

const readStoredValue = (key: string): string | null => {
  if (typeof window === 'undefined') {
    return null
  }

  return window.localStorage.getItem(key)
}

const writeStoredValue = (key: string, value: string | null) => {
  if (typeof window === 'undefined') {
    return
  }

  if (value === null) {
    window.localStorage.removeItem(key)
    return
  }

  window.localStorage.setItem(key, value)
}

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [token, setToken] = useState<string | null>(() => readStoredValue(TOKEN_STORAGE_KEY))
  const [user, setUser] = useState<AuthenticatedUser | null>(() => {
    const storedUser = readStoredValue(USER_STORAGE_KEY)
    if (!storedUser) {
      return null
    }

    try {
      return JSON.parse(storedUser) as AuthenticatedUser
    } catch {
      return null
    }
  })
  const [isAuthenticating, setIsAuthenticating] = useState(false)
  const [lastError, setLastError] = useState<ApiError | null>(null)

  useEffect(() => {
    setAuthToken(token)
  }, [token])

  const login = useCallback(async (credentials: LoginRequest) => {
    setIsAuthenticating(true)
    setLastError(null)
    try {
      const response = await authenticate(credentials)
      setToken(response.token)
      writeStoredValue(TOKEN_STORAGE_KEY, response.token)
      if (response.user) {
        setUser(response.user)
        writeStoredValue(USER_STORAGE_KEY, JSON.stringify(response.user))
      } else {
        setUser(null)
        writeStoredValue(USER_STORAGE_KEY, null)
      }
    } catch (error) {
      const apiError = error instanceof ApiError ? error : new ApiError('Unable to authenticate', 500, null)
      setLastError(apiError)
      throw apiError
    } finally {
      setIsAuthenticating(false)
    }
  }, [])

  const logout = useCallback(() => {
    setToken(null)
    setUser(null)
    setLastError(null)
    writeStoredValue(TOKEN_STORAGE_KEY, null)
    writeStoredValue(USER_STORAGE_KEY, null)
    setAuthToken(null)
  }, [])

  const value = useMemo(
    () => ({ token, user, isAuthenticating, login, logout, lastError }),
    [token, user, isAuthenticating, login, logout, lastError],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }

  return context
}
