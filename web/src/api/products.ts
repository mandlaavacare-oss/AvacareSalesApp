import { apiRequest } from './client'

export interface Product {
  id: string
  name: string
  sku?: string
  description?: string
  unitPrice?: number
  quantityOnHand?: number
}

export const listProducts = () => apiRequest<Product[]>('/products', { method: 'GET' })
