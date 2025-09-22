import { FormEvent, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'

export const LoginPage = () => {
  const { login, isAuthenticating, token, lastError } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (token) {
      navigate('/customers', { replace: true })
    }
  }, [token, navigate])

  useEffect(() => {
    if (lastError) {
      setError(lastError.message)
    }
  }, [lastError])

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    try {
      await login({ username, password })
      navigate('/customers', { replace: true })
    } catch (err) {
      const apiError = err as ApiError
      setError(apiError.message || 'Unable to authenticate. Please try again.')
    }
  }

  return (
    <div className="auth-card">
      <h2>Sign in</h2>
      <p>Authenticate with your Avacare credentials to access sales operations.</p>
      <form onSubmit={handleSubmit} className="auth-form">
        <label htmlFor="username">Username</label>
        <input
          id="username"
          name="username"
          type="text"
          value={username}
          autoComplete="username"
          onChange={(event) => setUsername(event.target.value)}
          required
        />
        <label htmlFor="password">Password</label>
        <input
          id="password"
          name="password"
          type="password"
          value={password}
          autoComplete="current-password"
          onChange={(event) => setPassword(event.target.value)}
          required
        />
        {error ? <div className="form-error">{error}</div> : null}
        <button type="submit" className="primary" disabled={isAuthenticating}>
          {isAuthenticating ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  )
}
