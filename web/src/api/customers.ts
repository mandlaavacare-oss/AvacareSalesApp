import { apiRequest } from './client'

export interface Customer {
  id: string
  name: string
  email?: string
  accountNumber?: string
  status?: string
  balance?: number
}

export const listCustomers = () => apiRequest<Customer[]>('/customers', { method: 'GET' })
