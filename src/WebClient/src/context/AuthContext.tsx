import { createContext, useContext, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import type { AuthSession, LoginCredentials } from '../types/auth'
import { authService } from '../services/authService'

interface AuthContextValue {
  session: AuthSession | null
  isAuthenticated: boolean
  loading: boolean
  login: (credentials: LoginCredentials) => Promise<AuthSession>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)

interface AuthProviderProps {
  children: ReactNode
}

export const AuthProvider = ({ children }: AuthProviderProps) => {
  const [session, setSession] = useState<AuthSession | null>(() => authService.getSession())
  const [loading, setLoading] = useState(false)

  const handleLogin = async (credentials: LoginCredentials) => {
    setLoading(true)
    try {
      const activeSession = await authService.login(credentials)
      setSession(activeSession)
      return activeSession
    } finally {
      setLoading(false)
    }
  }

  const handleLogout = () => {
    authService.logout()
    setSession(null)
  }

  const value = useMemo(
    () => ({
      session,
      isAuthenticated: Boolean(session),
      loading,
      login: handleLogin,
      logout: handleLogout,
    }),
    [session, loading],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
