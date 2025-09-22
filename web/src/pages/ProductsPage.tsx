import { useCallback, useEffect, useState } from 'react'
import { listProducts, Product } from '../api/products'
import { ApiError } from '../api/client'
import { EmptyState, ErrorState, LoadingState } from '../components/Feedback'

export const ProductsPage = () => {
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadProducts = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listProducts()
      setProducts(data)
    } catch (err) {
      const apiError = err as ApiError
      setError(apiError.message)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadProducts()
  }, [loadProducts])

  if (loading) {
    return <LoadingState message="Loading products…" />
  }

  if (error) {
    return <ErrorState message={error} retry={loadProducts} />
  }

  if (products.length === 0) {
    return <EmptyState title="No products" description="Products published in Evolution will be synchronised here." />
  }

  return (
    <div className="panel">
      <h2>Product Catalogue</h2>
      <table className="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>SKU</th>
            <th>Description</th>
            <th>Unit Price</th>
            <th>Stock</th>
          </tr>
        </thead>
        <tbody>
          {products.map((product) => (
            <tr key={product.id}>
              <td>{product.name}</td>
              <td>{product.sku ?? '—'}</td>
              <td>{product.description ?? '—'}</td>
              <td>{product.unitPrice?.toLocaleString(undefined, { style: 'currency', currency: 'USD' }) ?? '—'}</td>
              <td>{product.quantityOnHand ?? '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
