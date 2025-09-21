import { httpRequest } from '../api/httpClient'
import type { Product } from '../types/product'

const fallbackProducts: Product[] = [
  {
    id: 'sample-1',
    name: 'Wireless Barcode Scanner',
    sku: 'INV-001',
    description: 'Bluetooth-enabled scanner ideal for retail and warehouse environments.',
    price: 249.99,
    currency: 'USD',
    inStock: true,
    stockQuantity: 42,
    category: 'Hardware',
  },
  {
    id: 'sample-2',
    name: 'Thermal Shipping Labels (500 pack)',
    sku: 'INV-002',
    description: 'High-adhesive thermal labels compatible with major courier formats.',
    price: 39.5,
    currency: 'USD',
    inStock: true,
    stockQuantity: 18,
    category: 'Consumables',
  },
  {
    id: 'sample-3',
    name: 'Point of Sale Tablet Stand',
    sku: 'INV-003',
    description: 'Adjustable aluminium stand with cable management and security lock.',
    price: 179.99,
    currency: 'USD',
    inStock: false,
    stockQuantity: 0,
    category: 'Hardware',
  },
]

export const productService = {
  async getProducts(search?: string): Promise<Product[]> {
    const params = new URLSearchParams()
    if (search) {
      params.set('search', search)
    }

    const query = params.toString()

    try {
      return await httpRequest<Product[]>(`/products${query ? `?${query}` : ''}`)
    } catch (error) {
      console.warn('Falling back to sample product data until the API is available.', error)
      return fallbackProducts
    }
  },
  async addToCart(productId: string, quantity = 1): Promise<void> {
    await httpRequest('/cart/items', {
      method: 'POST',
      body: JSON.stringify({ productId, quantity }),
    })
  },
}
