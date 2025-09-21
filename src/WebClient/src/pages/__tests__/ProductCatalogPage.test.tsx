import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import ProductCatalogPage from '../ProductCatalogPage'
import { AuthContext } from '../../context/AuthContext'
import type { AuthSession } from '../../types/auth'
import type { Product } from '../../types/product'

vi.mock('../../services/productService', () => {
  const products: Product[] = [
    {
      id: '1',
      name: 'Barcode Scanner',
      sku: 'SCN-001',
      description: 'Wireless scanner',
      price: 199.99,
      currency: 'USD',
      inStock: true,
      stockQuantity: 10,
    },
    {
      id: '2',
      name: 'Thermal Printer',
      sku: 'PRN-002',
      description: 'Desktop printer',
      price: 299.99,
      currency: 'USD',
      inStock: false,
      stockQuantity: 0,
    },
  ]

  return {
    productService: {
      getProducts: vi.fn().mockResolvedValue(products),
      addToCart: vi.fn().mockResolvedValue(undefined),
    },
  }
})

import { productService } from '../../services/productService'

const mockedProductService = vi.mocked(productService, true)

describe('ProductCatalogPage', () => {

  const renderComponent = (session?: AuthSession | null) =>
    render(
      <AuthContext.Provider
        value={{
          session: session ?? {
            token: 'token',
            user: { id: 'user-1', email: 'sales@example.com', name: 'Sales Agent' },
          },
          isAuthenticated: true,
          loading: false,
          login: vi.fn(),
          logout: vi.fn(),
        }}
      >
        <MemoryRouter>
          <ProductCatalogPage />
        </MemoryRouter>
      </AuthContext.Provider>,
    )

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('renders products and filters by search term', async () => {
    renderComponent()

    expect(await screen.findByText(/barcode scanner/i)).toBeInTheDocument()
    expect(screen.getByText(/thermal printer/i)).toBeInTheDocument()

    await userEvent.type(screen.getByLabelText(/search/i), 'printer')

    await waitFor(() => {
      expect(screen.queryByText(/barcode scanner/i)).not.toBeInTheDocument()
      expect(screen.getByText(/thermal printer/i)).toBeInTheDocument()
    })
  })

  it('adds a product to the cart', async () => {
    renderComponent()

    const addButtons = await screen.findAllByRole('button', { name: /add to cart/i })
    await userEvent.click(addButtons[0])

    await waitFor(() => {
      expect(screen.getByRole('status')).toHaveTextContent(/was added to your cart/i)
    })

    expect(mockedProductService.addToCart).toHaveBeenCalledWith('1', 1)
  })
})
