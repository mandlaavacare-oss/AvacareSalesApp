import { useCallback, useEffect, useState } from 'react'
import { listInvoices, InvoiceSummary } from '../api/invoices'
import { ApiError } from '../api/client'
import { EmptyState, ErrorState, LoadingState } from '../components/Feedback'

export const InvoicesPage = () => {
  const [invoices, setInvoices] = useState<InvoiceSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadInvoices = useCallback(async () => {
    setLoading(true)
    setError(null)

    try {
      const data = await listInvoices()
      setInvoices(data)
    } catch (err) {
      const apiError = err as ApiError
      setError(apiError.message)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadInvoices()
  }, [loadInvoices])

  if (loading) {
    return <LoadingState message="Loading invoices…" />
  }

  if (error) {
    return <ErrorState message={error} retry={loadInvoices} />
  }

  if (invoices.length === 0) {
    return <EmptyState title="No invoices" description="Posted invoices will appear once order fulfilment is complete." />
  }

  return (
    <div className="panel">
      <h2>Accounts receivable invoices</h2>
      <table className="data-table">
        <thead>
          <tr>
            <th>Invoice #</th>
            <th>Order</th>
            <th>Status</th>
            <th>Total</th>
            <th>Balance due</th>
            <th>Issued</th>
            <th>Due</th>
          </tr>
        </thead>
        <tbody>
          {invoices.map((invoice) => (
            <tr key={invoice.id}>
              <td>{invoice.id}</td>
              <td>{invoice.orderId ?? '—'}</td>
              <td>{invoice.status ?? 'Open'}</td>
              <td>
                {invoice.totalAmount !== undefined
                  ? invoice.totalAmount.toLocaleString(undefined, { style: 'currency', currency: 'USD' })
                  : '—'}
              </td>
              <td>
                {invoice.balanceDue !== undefined
                  ? invoice.balanceDue.toLocaleString(undefined, { style: 'currency', currency: 'USD' })
                  : '—'}
              </td>
              <td>{invoice.issuedAt ? new Date(invoice.issuedAt).toLocaleDateString() : '—'}</td>
              <td>{invoice.dueAt ? new Date(invoice.dueAt).toLocaleDateString() : '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
