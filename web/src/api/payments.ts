import { apiRequest } from './client'

export interface PaymentRequest {
  invoiceId: string
  amount: number
  method?: string
  reference?: string
}

export interface PaymentSummary {
  id: string
  invoiceId: string
  amount: number
  receivedAt?: string
  method?: string
  reference?: string
}

export const listPayments = () => apiRequest<PaymentSummary[]>('/payments', { method: 'GET' })

export const recordPayment = (payload: PaymentRequest) =>
  apiRequest<PaymentSummary>('/payments', {
    method: 'POST',
    body: payload,
  })
