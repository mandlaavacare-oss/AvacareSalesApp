import { MemoryRouter } from 'react-router-dom'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, test, vi } from 'vitest'
import { AppRoutes } from '../App'
import { AuthProvider } from '../auth/AuthContext'

const jsonResponse = (data: unknown, init?: ResponseInit) =>
  Promise.resolve(
    new Response(JSON.stringify(data), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
      ...init,
    }),
  )

describe('Avacare sales app flows', () => {
  let fetchSpy: ReturnType<typeof vi.spyOn>

  beforeEach(() => {
    localStorage.clear()
    fetchSpy = vi.spyOn(global, 'fetch')
  })

  afterEach(() => {
    fetchSpy.mockRestore()
  })

  const renderWithProviders = (initialEntries = ['/login']) =>
    render(
      <AuthProvider>
        <MemoryRouter initialEntries={initialEntries}>
          <AppRoutes />
        </MemoryRouter>
      </AuthProvider>,
    )

  test('authenticates a user and loads customer records', async () => {
    const user = userEvent.setup()

    fetchSpy.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.url
      if (url.endsWith('/auth/login')) {
        return jsonResponse({ token: 'token-123', user: { id: 'user-1', name: 'Sales Agent' } })
      }
      if (url.endsWith('/customers')) {
        const headers = new Headers(init?.headers)
        expect(headers.get('Authorization')).toBe('Bearer token-123')
        return jsonResponse([
          { id: 'cust-1', name: 'Acme Medical', accountNumber: 'ACM001', email: 'accounts@acme.example' },
        ])
      }

      return Promise.reject(new Error(`Unhandled request: ${url}`))
    })

    renderWithProviders(['/login'])

    await user.type(screen.getByLabelText(/username/i), 'sales@example.com')
    await user.type(screen.getByLabelText(/password/i), 'secret')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    expect(await screen.findByText('Customer Directory')).toBeInTheDocument()
    expect(screen.getByText('Acme Medical')).toBeInTheDocument()
    expect(localStorage.getItem('avacare.authToken')).toBe('token-123')
  })

  test('captures an order and refreshes order list', async () => {
    const user = userEvent.setup()
    localStorage.setItem('avacare.authToken', 'token-xyz')
    localStorage.setItem('avacare.user', JSON.stringify({ id: 'user-2', name: 'Order Clerk' }))

    const customers = [{ id: 'cust-7', name: 'Mercury Labs' }]
    const products = [{ id: 'prod-9', name: 'Medical Kit', unitPrice: 299 }]
    const createdOrder = {
      id: 'order-100',
      customerId: 'cust-7',
      status: 'Pending',
      totalAmount: 598,
      createdAt: '2024-01-01T00:00:00.000Z',
      reference: 'ORD-100',
    }

    let orderFetchCount = 0

    fetchSpy.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.url
      if (url.endsWith('/customers')) {
        return jsonResponse(customers)
      }
      if (url.endsWith('/products')) {
        return jsonResponse(products)
      }
      if (url.endsWith('/orders') && (!init?.method || init.method === 'GET')) {
        orderFetchCount += 1
        return jsonResponse(orderFetchCount > 1 ? [createdOrder] : [])
      }
      if (url.endsWith('/orders') && init?.method === 'POST') {
        const headers = new Headers(init.headers)
        expect(headers.get('Authorization')).toBe('Bearer token-xyz')
        const payload = JSON.parse(init.body as string)
        expect(payload.customerId).toBe('cust-7')
        expect(payload.lines[0]).toMatchObject({ productId: 'prod-9', quantity: 2 })
        return jsonResponse(createdOrder, { status: 201 })
      }

      return Promise.reject(new Error(`Unhandled request: ${url}`))
    })

    render(
      <AuthProvider>
        <MemoryRouter initialEntries={['/orders']}>
          <AppRoutes />
        </MemoryRouter>
      </AuthProvider>,
    )

    await screen.findByText('Create sales order')

    await user.selectOptions(screen.getByLabelText(/customer/i), 'cust-7')
    await user.selectOptions(screen.getByLabelText(/product/i), 'prod-9')

    const quantityInput = screen.getByLabelText(/quantity/i)
    await user.clear(quantityInput)
    await user.type(quantityInput, '2')

    await user.click(screen.getByRole('button', { name: /create order/i }))

    await waitFor(() => expect(orderFetchCount).toBeGreaterThan(1))
    expect(await screen.findByText('ORD-100')).toBeInTheDocument()
    expect(screen.getByText('Pending')).toBeInTheDocument()
  })
})
