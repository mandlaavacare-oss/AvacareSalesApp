export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResult {
  username: string
  token: string
}

export interface Customer {
  id: string
  name: string
  email: string
  creditLimit: number
}

export interface Product {
  id: string
  name: string
  description: string
  price: number
  quantityOnHand: number
}

export interface SalesOrderLine {
  productId: string
  quantity: number
  unitPrice: number
}

export interface CreateOrderRequest {
  customerId: string
  orderDate: string
  lines: ReadonlyArray<SalesOrderLine>
}

export interface SalesOrder {
  id: string
  customerId: string
  orderDate: string
  lines: ReadonlyArray<SalesOrderLine>
}

export interface ApplyPaymentRequest {
  invoiceId: string
  amount: number
  paidOn: string
}

export interface Payment {
  id: string
  invoiceId: string
  amount: number
  paidOn: string
}
