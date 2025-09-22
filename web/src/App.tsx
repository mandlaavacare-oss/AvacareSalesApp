import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import {
  ApiError,
  applyPayment,
  createOrder,
  fetchCustomer,
  fetchProducts,
  getSession,
  login,
  logout,
  type ApplyPaymentRequest,
  type Customer,
  type LoginResult,
  type Payment,
  type Product,
  type SalesOrder,
} from './api/client'

interface OrderLineDraft {
  productId: string
  name: string
  quantity: number
  unitPrice: number
}

const getToday = () => new Date().toISOString().slice(0, 10)

const resolveErrorMessage = (error: unknown): string => {
  if (error instanceof ApiError) {
    return error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'An unexpected error occurred.'
}

function App() {
  const [session, setSession] = useState<LoginResult | null>(() => getSession())
  const isAuthenticated = Boolean(session)

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [isLoggingIn, setIsLoggingIn] = useState(false)
  const [loginError, setLoginError] = useState<string | null>(null)

  const [customerId, setCustomerId] = useState('')
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [customerError, setCustomerError] = useState<string | null>(null)
  const [customerLoading, setCustomerLoading] = useState(false)

  const [products, setProducts] = useState<ReadonlyArray<Product>>([])
  const [productError, setProductError] = useState<string | null>(null)
  const [productsLoading, setProductsLoading] = useState(false)

  const [orderCustomerId, setOrderCustomerId] = useState('')
  const [orderDate, setOrderDate] = useState(() => getToday())
  const [orderLines, setOrderLines] = useState<OrderLineDraft[]>([])
  const [orderError, setOrderError] = useState<string | null>(null)
  const [orderSuccess, setOrderSuccess] = useState<string | null>(null)
  const [orderSubmitting, setOrderSubmitting] = useState(false)
  const [latestOrder, setLatestOrder] = useState<SalesOrder | null>(null)

  const [invoiceId, setInvoiceId] = useState('')
  const [paymentAmount, setPaymentAmount] = useState('')
  const [paymentDate, setPaymentDate] = useState(() => getToday())
  const [paymentError, setPaymentError] = useState<string | null>(null)
  const [paymentSuccess, setPaymentSuccess] = useState<string | null>(null)
  const [paymentLoading, setPaymentLoading] = useState(false)
  const [latestPayment, setLatestPayment] = useState<Payment | null>(null)

  const orderTotal = useMemo(() => {
    return orderLines.reduce((total, line) => total + line.unitPrice * line.quantity, 0)
  }, [orderLines])

  const handleLogin = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (isLoggingIn) {
      return
    }

    setIsLoggingIn(true)
    setLoginError(null)

    try {
      const result = await login({ username, password })
      setSession(result)
      setUsername('')
      setPassword('')
    } catch (error) {
      setLoginError(resolveErrorMessage(error))
    } finally {
      setIsLoggingIn(false)
    }
  }

  const handleLogout = () => {
    logout()
    setSession(null)
    setCustomer(null)
    setProducts([])
    setOrderLines([])
    setLatestOrder(null)
    setLatestPayment(null)
    setOrderCustomerId('')
    setOrderDate(getToday())
    setOrderSuccess(null)
    setOrderError(null)
    setPaymentSuccess(null)
    setPaymentError(null)
    setInvoiceId('')
    setPaymentAmount('')
    setPaymentDate(getToday())
    setCustomerId('')
  }

  const handleCustomerLookup = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!isAuthenticated) {
      setCustomerError('Sign in to look up customer accounts.')
      return
    }

    if (!customerId.trim()) {
      setCustomerError('Enter a customer ID to search.')
      return
    }

    setCustomerError(null)
    setCustomerLoading(true)

    try {
      const result = await fetchCustomer(customerId.trim())
      setCustomer(result)
      setOrderCustomerId(result.id)
    } catch (error) {
      setCustomer(null)
      setCustomerError(resolveErrorMessage(error))
    } finally {
      setCustomerLoading(false)
    }
  }

  const handleLoadProducts = async () => {
    if (!isAuthenticated) {
      setProductError('Sign in to browse the catalogue.')
      return
    }

    setProductError(null)
    setProductsLoading(true)

    try {
      const result = await fetchProducts()
      setProducts(result)
    } catch (error) {
      setProducts([])
      setProductError(resolveErrorMessage(error))
    } finally {
      setProductsLoading(false)
    }
  }

  const handleAddProduct = (product: Product) => {
    setOrderError(null)
    setOrderSuccess(null)

    setOrderLines((previous) => {
      const existing = previous.find((line) => line.productId === product.id)
      if (existing) {
        return previous.map((line) =>
          line.productId === product.id
            ? { ...line, quantity: line.quantity + 1 }
            : line,
        )
      }

      return [
        ...previous,
        {
          productId: product.id,
          name: product.name,
          quantity: 1,
          unitPrice: product.price,
        },
      ]
    })
  }

  const handleQuantityChange = (productId: string, value: string) => {
    const parsed = Number(value)
    if (!Number.isFinite(parsed) || parsed <= 0) {
      return
    }

    setOrderLines((previous) =>
      previous.map((line) =>
        line.productId === productId ? { ...line, quantity: parsed } : line,
      ),
    )
  }

  const handleRemoveLine = (productId: string) => {
    setOrderLines((previous) => previous.filter((line) => line.productId !== productId))
  }

  const handleSubmitOrder = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!isAuthenticated) {
      setOrderError('Sign in before creating an order.')
      return
    }

    if (!orderCustomerId.trim()) {
      setOrderError('Provide the customer ID for this order.')
      return
    }

    if (orderLines.length === 0) {
      setOrderError('Add at least one product to the order.')
      return
    }

    setOrderError(null)
    setOrderSuccess(null)
    setOrderSubmitting(true)

    try {
      const payload = {
        customerId: orderCustomerId.trim(),
        orderDate: new Date(orderDate).toISOString(),
        lines: orderLines.map((line) => ({
          productId: line.productId,
          quantity: line.quantity,
          unitPrice: line.unitPrice,
        })),
      }

      const result = await createOrder(payload)
      setLatestOrder(result)
      setOrderSuccess(`Order ${result.id} created for ${result.customerId}.`)
      setOrderLines([])
    } catch (error) {
      setOrderError(resolveErrorMessage(error))
    } finally {
      setOrderSubmitting(false)
    }
  }

  const handleSubmitPayment = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!isAuthenticated) {
      setPaymentError('Sign in before applying payments.')
      return
    }

    if (!invoiceId.trim()) {
      setPaymentError('Enter an invoice ID to apply the payment to.')
      return
    }

    const amount = Number(paymentAmount)
    if (!Number.isFinite(amount) || amount <= 0) {
      setPaymentError('Enter a valid payment amount.')
      return
    }

    setPaymentError(null)
    setPaymentSuccess(null)
    setPaymentLoading(true)

    const request: ApplyPaymentRequest = {
      invoiceId: invoiceId.trim(),
      amount,
      paidOn: new Date(paymentDate).toISOString(),
    }

    try {
      const result = await applyPayment(request)
      setLatestPayment(result)
      setPaymentSuccess(`Payment ${result.id} applied to invoice ${result.invoiceId}.`)
      setInvoiceId('')
      setPaymentAmount('')
    } catch (error) {
      setPaymentError(resolveErrorMessage(error))
    } finally {
      setPaymentLoading(false)
    }
  }

  return (
    <div className="app">
      <header className="app__header">
        <h1>Avacare Sales Console</h1>
        <p>
          {isAuthenticated
            ? `Signed in as ${session?.username}. Use the tools below to manage customers, orders, and payments.`
            : 'Sign in to retrieve customers, review inventory, create sales orders, and apply payments.'}
        </p>
        {isAuthenticated ? (
          <button type="button" className="button-secondary" onClick={handleLogout}>
            Sign out
          </button>
        ) : null}
      </header>

      <section className="panel" aria-labelledby="auth-heading">
        <div className="panel__header">
          <h2 id="auth-heading">Authentication</h2>
          <p className="panel__description">
            Connect with your Sage-backed credentials to access transaction services.
          </p>
        </div>

        {isAuthenticated ? (
          <div className="status-box status-box--success">
            <p>Signed in as {session?.username}.</p>
            <p>You can sign out at any time using the button above.</p>
          </div>
        ) : (
          <form className="form-grid" onSubmit={handleLogin} noValidate>
            <div className="field">
              <label htmlFor="username">Username</label>
              <input
                id="username"
                name="username"
                type="text"
                autoComplete="username"
                value={username}
                onChange={(event) => setUsername(event.target.value)}
                required
              />
            </div>
            <div className="field">
              <label htmlFor="password">Password</label>
              <input
                id="password"
                name="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
            </div>
            <div className="form-actions">
              <button type="submit" disabled={isLoggingIn}>
                {isLoggingIn ? 'Signing in…' : 'Sign in'}
              </button>
            </div>
          </form>
        )}

        {loginError ? (
          <p role="alert" className="status-message status-message--error">
            {loginError}
          </p>
        ) : null}
      </section>

      <section className="panel" aria-labelledby="customer-heading">
        <div className="panel__header">
          <h2 id="customer-heading">Customer lookup</h2>
          <p className="panel__description">Identify a customer by their account code to review credit details.</p>
        </div>
        <form className="form-inline" onSubmit={handleCustomerLookup} noValidate>
          <div className="field">
            <label htmlFor="customerId">Customer ID</label>
            <input
              id="customerId"
              name="customerId"
              type="text"
              value={customerId}
              onChange={(event) => setCustomerId(event.target.value)}
              disabled={!isAuthenticated || customerLoading}
              required
            />
          </div>
          <button type="submit" disabled={!isAuthenticated || customerLoading}>
            {customerLoading ? 'Looking up…' : 'Lookup'}
          </button>
        </form>
        {!isAuthenticated ? (
          <p className="status-message">Sign in to access customer records.</p>
        ) : null}
        {customerError ? (
          <p role="alert" className="status-message status-message--error">
            {customerError}
          </p>
        ) : null}
        {customer ? (
          <dl className="details-grid">
            <div>
              <dt>Name</dt>
              <dd>{customer.name}</dd>
            </div>
            <div>
              <dt>Customer ID</dt>
              <dd>{customer.id}</dd>
            </div>
            <div>
              <dt>Email</dt>
              <dd>{customer.email}</dd>
            </div>
            <div>
              <dt>Credit limit</dt>
              <dd>{customer.creditLimit.toLocaleString(undefined, { style: 'currency', currency: 'USD' })}</dd>
            </div>
          </dl>
        ) : null}
      </section>

      <section className="panel" aria-labelledby="products-heading">
        <div className="panel__header">
          <h2 id="products-heading">Product catalogue</h2>
          <p className="panel__description">
            Review available stock items and add them to a draft sales order.
          </p>
        </div>
        <div className="panel__actions">
          <button type="button" onClick={handleLoadProducts} disabled={!isAuthenticated || productsLoading}>
            {productsLoading ? 'Loading products…' : 'Load products'}
          </button>
        </div>
        {productError ? (
          <p role="alert" className="status-message status-message--error">
            {productError}
          </p>
        ) : null}
        {!isAuthenticated ? (
          <p className="status-message">Sign in to inspect available inventory.</p>
        ) : null}
        {isAuthenticated && products.length === 0 && !productsLoading ? (
          <p className="status-message">Select “Load products” to fetch the current catalogue.</p>
        ) : null}
        {products.length > 0 ? (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col">Product ID</th>
                  <th scope="col">Price</th>
                  <th scope="col">Qty. on hand</th>
                  <th scope="col" className="data-table__actions">
                    <span className="sr-only">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {products.map((product) => (
                  <tr key={product.id}>
                    <td>{product.name}</td>
                    <td>{product.id}</td>
                    <td>{product.price.toLocaleString(undefined, { style: 'currency', currency: 'USD' })}</td>
                    <td>{product.quantityOnHand}</td>
                    <td className="data-table__actions">
                      <button
                        type="button"
                        className="button-secondary"
                        onClick={() => handleAddProduct(product)}
                        disabled={!isAuthenticated}
                      >
                        Add to order
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
      </section>

      <section className="panel" aria-labelledby="order-heading">
        <div className="panel__header">
          <h2 id="order-heading">Create sales order</h2>
          <p className="panel__description">
            Confirm order details and submit them to the Order Entry service.
          </p>
        </div>
        <form className="form-grid" onSubmit={handleSubmitOrder} noValidate>
          <div className="field">
            <label htmlFor="orderCustomerId">Order customer ID</label>
            <input
              id="orderCustomerId"
              name="orderCustomerId"
              type="text"
              value={orderCustomerId}
              onChange={(event) => setOrderCustomerId(event.target.value)}
              disabled={!isAuthenticated || orderSubmitting}
              required
            />
          </div>
          <div className="field">
            <label htmlFor="orderDate">Order date</label>
            <input
              id="orderDate"
              name="orderDate"
              type="date"
              value={orderDate}
              onChange={(event) => setOrderDate(event.target.value)}
              disabled={!isAuthenticated || orderSubmitting}
              required
            />
          </div>
          <div className="form-actions">
            <button type="submit" disabled={!isAuthenticated || orderSubmitting || orderLines.length === 0}>
              {orderSubmitting ? 'Submitting order…' : 'Submit order'}
            </button>
          </div>
        </form>
        {orderLines.length === 0 ? (
          <p className="status-message">Add products to build the order lines.</p>
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th scope="col">Product</th>
                  <th scope="col">Product ID</th>
                  <th scope="col">Quantity</th>
                  <th scope="col">Unit price</th>
                  <th scope="col">Line total</th>
                  <th scope="col" className="data-table__actions">
                    <span className="sr-only">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {orderLines.map((line) => (
                  <tr key={line.productId}>
                    <td>{line.name}</td>
                    <td>{line.productId}</td>
                    <td>
                      <input
                        type="number"
                        min="1"
                        step="1"
                        value={line.quantity}
                        onChange={(event) => handleQuantityChange(line.productId, event.target.value)}
                        disabled={!isAuthenticated || orderSubmitting}
                        aria-label={`Quantity for ${line.name}`}
                      />
                    </td>
                    <td>{line.unitPrice.toLocaleString(undefined, { style: 'currency', currency: 'USD' })}</td>
                    <td>
                      {(line.unitPrice * line.quantity).toLocaleString(undefined, {
                        style: 'currency',
                        currency: 'USD',
                      })}
                    </td>
                    <td className="data-table__actions">
                      <button
                        type="button"
                        className="button-secondary"
                        onClick={() => handleRemoveLine(line.productId)}
                        disabled={orderSubmitting}
                      >
                        Remove
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <td colSpan={4}>Order total</td>
                  <td>{orderTotal.toLocaleString(undefined, { style: 'currency', currency: 'USD' })}</td>
                  <td />
                </tr>
              </tfoot>
            </table>
          </div>
        )}
        {orderError ? (
          <p role="alert" className="status-message status-message--error">
            {orderError}
          </p>
        ) : null}
        {orderSuccess ? (
          <p role="status" className="status-message status-message--success">
            {orderSuccess}
          </p>
        ) : null}
        {latestOrder ? (
          <p className="status-message">
            Last order ID: <strong>{latestOrder.id}</strong> on{' '}
            {new Date(latestOrder.orderDate).toLocaleString()}
          </p>
        ) : null}
      </section>

      <section className="panel" aria-labelledby="payment-heading">
        <div className="panel__header">
          <h2 id="payment-heading">Apply payment</h2>
          <p className="panel__description">
            Post a receipt against an outstanding invoice once funds have cleared.
          </p>
        </div>
        <form className="form-grid" onSubmit={handleSubmitPayment} noValidate>
          <div className="field">
            <label htmlFor="invoiceId">Invoice ID</label>
            <input
              id="invoiceId"
              name="invoiceId"
              type="text"
              value={invoiceId}
              onChange={(event) => setInvoiceId(event.target.value)}
              disabled={!isAuthenticated || paymentLoading}
              required
            />
          </div>
          <div className="field">
            <label htmlFor="paymentAmount">Payment amount</label>
            <input
              id="paymentAmount"
              name="paymentAmount"
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              value={paymentAmount}
              onChange={(event) => setPaymentAmount(event.target.value)}
              disabled={!isAuthenticated || paymentLoading}
              required
            />
          </div>
          <div className="field">
            <label htmlFor="paymentDate">Payment date</label>
            <input
              id="paymentDate"
              name="paymentDate"
              type="date"
              value={paymentDate}
              onChange={(event) => setPaymentDate(event.target.value)}
              disabled={!isAuthenticated || paymentLoading}
              required
            />
          </div>
          <div className="form-actions">
            <button type="submit" disabled={!isAuthenticated || paymentLoading}>
              {paymentLoading ? 'Applying payment…' : 'Apply payment'}
            </button>
          </div>
        </form>
        {paymentError ? (
          <p role="alert" className="status-message status-message--error">
            {paymentError}
          </p>
        ) : null}
        {paymentSuccess ? (
          <p role="status" className="status-message status-message--success">
            {paymentSuccess}
          </p>
        ) : null}
        {latestPayment ? (
          <p className="status-message">
            Receipt {latestPayment.id} posted for{' '}
            {latestPayment.amount.toLocaleString(undefined, { style: 'currency', currency: 'USD' })} on{' '}
            {new Date(latestPayment.paidOn).toLocaleDateString()}.
          </p>
        ) : null}
      </section>
    </div>
  )
}

export default App
