export interface Product {
  id: string
  name: string
  sku: string
  description?: string
  price: number
  currency: string
  inStock: boolean
  stockQuantity?: number
  category?: string
}
