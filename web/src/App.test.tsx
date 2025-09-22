import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import App from './App'
import { logout } from './api/client'

const originalFetch = globalThis.fetch

describe('Avacare Sales Console', () => {
  beforeEach(() => {
    localStorage.clear()
    logout()
    vi.restoreAllMocks()
    globalThis.fetch = originalFetch
  })

  it('authenticates and retrieves customer details', async () => {
    const user = userEvent.setup()

    const fetchMock = vi
      .fn(async (input: RequestInfo, init?: RequestInit) => {
        const url = typeof input === 'string' ? input : input.url

        if (url === '/auth/login') {
          expect(init?.method).toBe('POST')
          const headers = new Headers(init?.headers as HeadersInit)
          expect(headers.get('content-type')).toContain('application/json')
          const body = JSON.parse(init?.body as string)
          expect(body).toEqual({ username: 'demo', password: 'secret' })

          return new Response(
            JSON.stringify({ username: 'demo', token: 'test-token' }),
            {
              status: 200,
              headers: { 'Content-Type': 'application/json' },
            },
          )
        }

        if (url === '/customers/C001') {
          const headers = new Headers(init?.headers as HeadersInit)
          expect(headers.get('authorization')).toBe('Bearer test-token')

          return new Response(
            JSON.stringify({
              id: 'C001',
              name: 'Acme Corporation',
              email: 'billing@acme.com',
              creditLimit: 5000,
            }),
            {
              status: 200,
              headers: { 'Content-Type': 'application/json' },
            },
          )
        }

        throw new Error(`Unhandled request to ${url}`)
      })
      .mockName('fetch')

    globalThis.fetch = fetchMock as unknown as typeof fetch

    render(<App />)

    await user.type(screen.getByLabelText(/username/i), 'demo')
    await user.type(screen.getByLabelText(/password/i), 'secret')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await screen.findByText(/You can sign out at any time using the button above/i)

    await user.type(screen.getByLabelText(/^Customer ID$/i), 'C001')
    await user.click(screen.getByRole('button', { name: /lookup/i }))

    await screen.findByText(/Acme Corporation/)
    expect(screen.getByText(/Credit limit/i)).toBeInTheDocument()

    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('creates an order and applies a payment', async () => {
    const user = userEvent.setup()

    const fetchMock = vi
      .fn(async (input: RequestInfo, init?: RequestInit) => {
        const url = typeof input === 'string' ? input : input.url

        if (url === '/auth/login') {
          return new Response(
            JSON.stringify({ username: 'agent', token: 'order-token' }),
            {
              status: 200,
              headers: { 'Content-Type': 'application/json' },
            },
          )
        }

        if (url === '/products') {
          const headers = new Headers(init?.headers as HeadersInit)
          expect(headers.get('authorization')).toBe('Bearer order-token')

          return new Response(
            JSON.stringify([
              {
                id: 'P-100',
                name: 'Widget',
                description: 'Standard widget',
                price: 25,
                quantityOnHand: 10,
              },
            ]),
            {
              status: 200,
              headers: { 'Content-Type': 'application/json' },
            },
          )
        }

        if (url === '/orders') {
          const headers = new Headers(init?.headers as HeadersInit)
          expect(headers.get('authorization')).toBe('Bearer order-token')

          const body = JSON.parse(init?.body as string)
          expect(body).toEqual({
            customerId: 'CUST-777',
            orderDate: new Date('2024-01-15').toISOString(),
            lines: [
              {
                productId: 'P-100',
                quantity: 1,
                unitPrice: 25,
              },
            ],
          })

          return new Response(
            JSON.stringify({
              id: 'ORDER-1',
              customerId: 'CUST-777',
              orderDate: new Date('2024-01-15').toISOString(),
              lines: [
                {
                  productId: 'P-100',
                  quantity: 1,
                  unitPrice: 25,
                },
              ],
            }),
            {
              status: 200,
              headers: { 'Content-Type': 'application/json' },
            },
          )
        }

        if (url === '/payments') {
          const headers = new Headers(init?.headers as HeadersInit)
          expect(headers.get('authorization')).toBe('Bearer order-token')

          const body = JSON.parse(init?.body as string)
          expect(body).toEqual({
            invoiceId: 'INV-9',
            amount: 100,
            paidOn: new Date('2024-01-20').toISOString(),
          })

          return new Response(
            JSON.stringify({
              id: 'PAY-1',
              invoiceId: 'INV-9',
              amount: 100,
              paidOn: new Date('2024-01-20').toISOString(),
            }),
            {
              status: 200,
              headers: { 'Content-Type': 'application/json' },
            },
          )
        }

        throw new Error(`Unhandled request to ${url}`)
      })
      .mockName('fetch')

    globalThis.fetch = fetchMock as unknown as typeof fetch

    render(<App />)

    await user.type(screen.getByLabelText(/username/i), 'agent')
    await user.type(screen.getByLabelText(/password/i), 'letmein')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await screen.findByRole('button', { name: /sign out/i })

    await user.click(screen.getByRole('button', { name: /load products/i }))

    const productRow = await screen.findByRole('row', { name: /Widget/ })
    await user.click(within(productRow).getByRole('button', { name: /add to order/i }))

    const orderCustomerInput = screen.getByLabelText(/order customer id/i)
    await user.clear(orderCustomerInput)
    await user.type(orderCustomerInput, 'CUST-777')

    const orderDateInput = screen.getByLabelText(/order date/i)
    await user.clear(orderDateInput)
    await user.type(orderDateInput, '2024-01-15')

    await user.click(screen.getByRole('button', { name: /submit order/i }))
    await screen.findByText(/Order ORDER-1 created/i)

    const invoiceInput = screen.getByLabelText(/invoice id/i)
    await user.clear(invoiceInput)
    await user.type(invoiceInput, 'INV-9')

    const amountInput = screen.getByLabelText(/payment amount/i)
    await user.clear(amountInput)
    await user.type(amountInput, '100')

    const paymentDateInput = screen.getByLabelText(/payment date/i)
    await user.clear(paymentDateInput)
    await user.type(paymentDateInput, '2024-01-20')

    await user.click(screen.getByRole('button', { name: /apply payment/i }))
    await screen.findByText(/Payment PAY-1 applied/i)

    expect(fetchMock).toHaveBeenCalledTimes(4)
  })
})
