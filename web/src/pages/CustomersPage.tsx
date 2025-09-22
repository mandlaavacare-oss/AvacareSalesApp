import { useCallback, useEffect, useState } from 'react'
import { listCustomers, Customer } from '../api/customers'
import { ApiError } from '../api/client'
import { EmptyState, ErrorState, LoadingState } from '../components/Feedback'

export const CustomersPage = () => {
  const [customers, setCustomers] = useState<Customer[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadCustomers = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listCustomers()
      setCustomers(data)
    } catch (err) {
      const apiError = err as ApiError
      setError(apiError.message)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadCustomers()
  }, [loadCustomers])

  if (loading) {
    return <LoadingState message="Loading customers…" />
  }

  if (error) {
    return <ErrorState message={error} retry={loadCustomers} />
  }

  if (customers.length === 0) {
    return <EmptyState title="No customers" description="Once customers are synchronised they will appear here." />
  }

  return (
    <div className="panel">
      <h2>Customer Directory</h2>
      <table className="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Account #</th>
            <th>Email</th>
            <th>Status</th>
            <th>Balance</th>
          </tr>
        </thead>
        <tbody>
          {customers.map((customer) => (
            <tr key={customer.id}>
              <td>{customer.name}</td>
              <td>{customer.accountNumber ?? '—'}</td>
              <td>{customer.email ?? '—'}</td>
              <td>{customer.status ?? 'Active'}</td>
              <td>{customer.balance?.toLocaleString(undefined, { style: 'currency', currency: 'USD' }) ?? '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
