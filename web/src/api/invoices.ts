import { apiRequest } from './client'

export interface InvoiceSummary {
  id: string
  orderId?: string
  customerId?: string
  status?: string
  totalAmount?: number
  balanceDue?: number
  issuedAt?: string
  dueAt?: string
}

export const listInvoices = () => apiRequest<InvoiceSummary[]>('/invoices', { method: 'GET' })
