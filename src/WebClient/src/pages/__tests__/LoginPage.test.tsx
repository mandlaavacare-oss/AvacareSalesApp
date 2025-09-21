import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import LoginPage from '../LoginPage'
import { AuthContext } from '../../context/AuthContext'
import type { AuthSession } from '../../types/auth'

describe('LoginPage', () => {
  const renderWithContext = (loginMock: (credentials: { email: string; password: string }) => Promise<AuthSession>) => {
    return render(
      <AuthContext.Provider
        value={{
          session: null,
          isAuthenticated: false,
          loading: false,
          login: loginMock,
          logout: vi.fn(),
        }}
      >
        <MemoryRouter initialEntries={['/login']}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/catalog" element={<div>Catalog page</div>} />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>,
    )
  }

  it('submits credentials and navigates to the catalog', async () => {
    const loginMock = vi.fn().mockResolvedValue({
      token: 'token-123',
      user: { id: 'user-1', email: 'user@example.com' },
    })

    renderWithContext(loginMock)

    await userEvent.type(screen.getByLabelText(/email address/i), 'user@example.com')
    await userEvent.type(screen.getByLabelText(/password/i), 'password123')

    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(loginMock).toHaveBeenCalledWith({ email: 'user@example.com', password: 'password123' })
      expect(screen.getByText(/catalog page/i)).toBeInTheDocument()
    })
  })

  it('shows an error when the login fails', async () => {
    const loginMock = vi.fn().mockRejectedValue(new Error('Invalid credentials'))

    renderWithContext(loginMock)

    await userEvent.type(screen.getByLabelText(/email address/i), 'invalid@example.com')
    await userEvent.type(screen.getByLabelText(/password/i), 'wrong')

    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/invalid credentials/i)
    })
  })
})
