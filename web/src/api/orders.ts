import { apiRequest } from './client'

export interface OrderLineRequest {
  productId: string
  quantity: number
  unitPrice?: number
}

export interface CreateOrderRequest {
  customerId: string
  lines: OrderLineRequest[]
  reference?: string
  notes?: string
}

export interface OrderSummary {
  id: string
  customerId: string
  status?: string
  totalAmount?: number
  createdAt?: string
  reference?: string
}

export const listOrders = () => apiRequest<OrderSummary[]>('/orders', { method: 'GET' })

export const createOrder = (payload: CreateOrderRequest) =>
  apiRequest<OrderSummary>('/orders', {
    method: 'POST',
    body: payload,
  })
