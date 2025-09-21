import { useState } from 'react'
import type { FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import type { Location } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import type { LoginCredentials } from '../types/auth'

const LoginPage = () => {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [credentials, setCredentials] = useState<LoginCredentials>({ email: '', password: '' })
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const from = (location.state as { from?: Location } | undefined)?.from?.pathname ?? '/catalog'

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = event.target
    setCredentials((current) => ({ ...current, [name]: value }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    try {
      await login(credentials)
      navigate(from, { replace: true })
    } catch (submissionError) {
      setError((submissionError as Error).message || 'Unable to sign in. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="container py-5">
      <div className="row justify-content-center">
        <div className="col-md-6 col-lg-5">
          <div className="card shadow-sm border-0">
            <div className="card-body p-4 p-lg-5">
              <h1 className="h3 mb-4 text-center text-brand">Sign in to continue</h1>
              {error && (
                <div className="alert alert-danger" role="alert">
                  {error}
                </div>
              )}
              <form onSubmit={handleSubmit} noValidate>
                <div className="mb-3">
                  <label htmlFor="email" className="form-label">
                    Email address
                  </label>
                  <input
                    id="email"
                    name="email"
                    type="email"
                    className="form-control"
                    value={credentials.email}
                    onChange={handleChange}
                    autoComplete="username"
                    required
                  />
                </div>
                <div className="mb-4">
                  <label htmlFor="password" className="form-label">
                    Password
                  </label>
                  <input
                    id="password"
                    name="password"
                    type="password"
                    className="form-control"
                    value={credentials.password}
                    onChange={handleChange}
                    autoComplete="current-password"
                    required
                  />
                </div>
                <button
                  type="submit"
                  className="btn btn-primary w-100"
                  disabled={isSubmitting || !credentials.email || !credentials.password}
                >
                  {isSubmitting ? 'Signing in…' : 'Sign in'}
                </button>
              </form>
            </div>
          </div>
          <p className="text-center text-muted small mt-3">
            Tokens are stored securely for this session only. Close the browser to clear your session.
          </p>
        </div>
      </div>
    </div>
  )
}

export default LoginPage
