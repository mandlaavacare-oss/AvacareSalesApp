import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react'
import { listCustomers, Customer } from '../api/customers'
import { listProducts, Product } from '../api/products'
import { createOrder, listOrders, CreateOrderRequest, OrderSummary } from '../api/orders'
import { ApiError } from '../api/client'
import { EmptyState, ErrorState, LoadingState } from '../components/Feedback'

interface OrderFormState {
  customerId: string
  productId: string
  quantity: number
  notes: string
}

const defaultFormState: OrderFormState = {
  customerId: '',
  productId: '',
  quantity: 1,
  notes: '',
}

export const OrdersPage = () => {
  const [orders, setOrders] = useState<OrderSummary[]>([])
  const [customers, setCustomers] = useState<Customer[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [form, setForm] = useState<OrderFormState>(defaultFormState)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const loadData = useCallback(async () => {
    setLoading(true)
    setError(null)

    try {
      const [customerData, productData, orderData] = await Promise.all([
        listCustomers(),
        listProducts(),
        listOrders(),
      ])

      setCustomers(customerData)
      setProducts(productData)
      setOrders(orderData)
    } catch (err) {
      const apiError = err as ApiError
      setError(apiError.message)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadData()
  }, [loadData])

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setFormError(null)

    if (!form.customerId || !form.productId) {
      setFormError('Please select a customer and product.')
      return
    }

    if (Number.isNaN(form.quantity) || form.quantity <= 0) {
      setFormError('Quantity must be at least 1.')
      return
    }

    const payload: CreateOrderRequest = {
      customerId: form.customerId,
      notes: form.notes || undefined,
      lines: [
        {
          productId: form.productId,
          quantity: form.quantity,
        },
      ],
    }

    setSubmitting(true)

    try {
      await createOrder(payload)
      setForm(defaultFormState)
      await loadData()
    } catch (err) {
      const apiError = err as ApiError
      setFormError(apiError.message)
    } finally {
      setSubmitting(false)
    }
  }

  const productLookup = useMemo(() => {
    const lookup = new Map<string, Product>()
    products.forEach((product) => lookup.set(product.id, product))
    return lookup
  }, [products])

  if (loading) {
    return <LoadingState message="Loading order entry data…" />
  }

  if (error) {
    return <ErrorState message={error} retry={loadData} />
  }

  const selectedProduct = form.productId ? productLookup.get(form.productId) : undefined

  return (
    <div className="panel">
      <h2>Create sales order</h2>
      <form className="order-form" onSubmit={handleSubmit}>
        <div className="form-row">
          <label htmlFor="order-customer">Customer</label>
          <select
            id="order-customer"
            value={form.customerId}
            onChange={(event) => setForm((state) => ({ ...state, customerId: event.target.value }))}
            required
          >
            <option value="">Select customer</option>
            {customers.map((customer) => (
              <option key={customer.id} value={customer.id}>
                {customer.name}
              </option>
            ))}
          </select>
        </div>
        <div className="form-row">
          <label htmlFor="order-product">Product</label>
          <select
            id="order-product"
            value={form.productId}
            onChange={(event) => setForm((state) => ({ ...state, productId: event.target.value }))}
            required
          >
            <option value="">Select product</option>
            {products.map((product) => (
              <option key={product.id} value={product.id}>
                {product.name}
              </option>
            ))}
          </select>
        </div>
        <div className="form-row">
          <label htmlFor="order-quantity">Quantity</label>
          <input
            id="order-quantity"
            type="number"
            min={1}
            value={form.quantity}
            onChange={(event) => setForm((state) => ({ ...state, quantity: Number(event.target.value) }))}
            required
          />
        </div>
        <div className="form-row">
          <label htmlFor="order-notes">Notes</label>
          <textarea
            id="order-notes"
            value={form.notes}
            onChange={(event) => setForm((state) => ({ ...state, notes: event.target.value }))}
            placeholder="Delivery instructions, references, etc."
          />
        </div>
        {formError ? <div className="form-error">{formError}</div> : null}
        <button type="submit" className="primary" disabled={submitting}>
          {submitting ? 'Submitting…' : 'Create order'}
        </button>
      </form>

      <section className="orders-list">
        <h3>Recent orders</h3>
        {orders.length === 0 ? (
          <EmptyState
            title="No orders yet"
            description="Use the form above to capture a new sales order."
          />
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>Order #</th>
                <th>Customer</th>
                <th>Status</th>
                <th>Total</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => (
                <tr key={order.id}>
                  <td>{order.reference ?? order.id}</td>
                  <td>{customers.find((customer) => customer.id === order.customerId)?.name ?? order.customerId}</td>
                  <td>{order.status ?? 'Draft'}</td>
                  <td>
                    {order.totalAmount !== undefined
                      ? order.totalAmount.toLocaleString(undefined, { style: 'currency', currency: 'USD' })
                      : '—'}
                  </td>
                  <td>{order.createdAt ? new Date(order.createdAt).toLocaleString() : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
      <section className="products-summary">
        <h3>Product details</h3>
        {selectedProduct ? (
          <div className="product-card">
            <h4>{selectedProduct.name}</h4>
            <dl>
              <div>
                <dt>SKU</dt>
                <dd>{selectedProduct.sku ?? '—'}</dd>
              </div>
              <div>
                <dt>Unit price</dt>
                <dd>
                  {selectedProduct.unitPrice !== undefined
                    ? selectedProduct.unitPrice.toLocaleString(undefined, { style: 'currency', currency: 'USD' })
                    : '—'}
                </dd>
              </div>
              <div>
                <dt>Stock available</dt>
                <dd>{selectedProduct.quantityOnHand ?? '—'}</dd>
              </div>
            </dl>
          </div>
        ) : (
          <p>Select a product to see pricing and stock availability.</p>
        )}
      </section>
    </div>
  )
}
