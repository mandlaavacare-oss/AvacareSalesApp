import { FormEvent, useCallback, useEffect, useState } from 'react'
import { listInvoices, InvoiceSummary } from '../api/invoices'
import { listPayments, recordPayment, PaymentSummary } from '../api/payments'
import { ApiError } from '../api/client'
import { EmptyState, ErrorState, LoadingState } from '../components/Feedback'

interface PaymentFormState {
  invoiceId: string
  amount: number
  method: string
  reference: string
}

const defaultPaymentForm: PaymentFormState = {
  invoiceId: '',
  amount: 0,
  method: 'EFT',
  reference: '',
}

export const PaymentsPage = () => {
  const [payments, setPayments] = useState<PaymentSummary[]>([])
  const [invoices, setInvoices] = useState<InvoiceSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<PaymentFormState>(defaultPaymentForm)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const loadData = useCallback(async () => {
    setLoading(true)
    setError(null)

    try {
      const [invoiceData, paymentData] = await Promise.all([listInvoices(), listPayments()])
      setInvoices(invoiceData)
      setPayments(paymentData)
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

    if (!form.invoiceId) {
      setFormError('Please select an invoice to allocate the payment against.')
      return
    }

    if (Number.isNaN(form.amount) || form.amount <= 0) {
      setFormError('Amount must be greater than zero.')
      return
    }

    setSubmitting(true)

    try {
      await recordPayment({
        invoiceId: form.invoiceId,
        amount: form.amount,
        method: form.method || undefined,
        reference: form.reference || undefined,
      })
      setForm(defaultPaymentForm)
      await loadData()
    } catch (err) {
      const apiError = err as ApiError
      setFormError(apiError.message)
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) {
    return <LoadingState message="Loading payment data…" />
  }

  if (error) {
    return <ErrorState message={error} retry={loadData} />
  }

  return (
    <div className="panel">
      <h2>Record customer payment</h2>
      <form className="payment-form" onSubmit={handleSubmit}>
        <div className="form-row">
          <label htmlFor="payment-invoice">Invoice</label>
          <select
            id="payment-invoice"
            value={form.invoiceId}
            onChange={(event) => setForm((state) => ({ ...state, invoiceId: event.target.value }))}
            required
          >
            <option value="">Select invoice</option>
            {invoices.map((invoice) => (
              <option key={invoice.id} value={invoice.id}>
                {invoice.id} — {invoice.customerId ?? 'Unknown customer'}
              </option>
            ))}
          </select>
        </div>
        <div className="form-row">
          <label htmlFor="payment-amount">Amount</label>
          <input
            id="payment-amount"
            type="number"
            min={0}
            step="0.01"
            value={form.amount}
            onChange={(event) => setForm((state) => ({ ...state, amount: Number(event.target.value) }))}
            required
          />
        </div>
        <div className="form-row">
          <label htmlFor="payment-method">Method</label>
          <input
            id="payment-method"
            type="text"
            value={form.method}
            onChange={(event) => setForm((state) => ({ ...state, method: event.target.value }))}
            placeholder="EFT, Cash, Card, etc."
          />
        </div>
        <div className="form-row">
          <label htmlFor="payment-reference">Reference</label>
          <input
            id="payment-reference"
            type="text"
            value={form.reference}
            onChange={(event) => setForm((state) => ({ ...state, reference: event.target.value }))}
            placeholder="Receipt reference"
          />
        </div>
        {formError ? <div className="form-error">{formError}</div> : null}
        <button type="submit" className="primary" disabled={submitting}>
          {submitting ? 'Saving…' : 'Allocate payment'}
        </button>
      </form>

      <section className="payments-list">
        <h3>Recent payments</h3>
        {payments.length === 0 ? (
          <EmptyState
            title="No payments captured"
            description="Once payments are recorded they will appear here."
          />
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>Payment #</th>
                <th>Invoice</th>
                <th>Amount</th>
                <th>Method</th>
                <th>Received</th>
                <th>Reference</th>
              </tr>
            </thead>
            <tbody>
              {payments.map((payment) => (
                <tr key={payment.id}>
                  <td>{payment.id}</td>
                  <td>{payment.invoiceId}</td>
                  <td>{payment.amount.toLocaleString(undefined, { style: 'currency', currency: 'USD' })}</td>
                  <td>{payment.method ?? '—'}</td>
                  <td>{payment.receivedAt ? new Date(payment.receivedAt).toLocaleString() : '—'}</td>
                  <td>{payment.reference ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  )
}
