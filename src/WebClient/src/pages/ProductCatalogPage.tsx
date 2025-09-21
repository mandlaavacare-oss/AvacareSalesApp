import { useEffect, useMemo, useState } from 'react'
import ProductCard from '../components/ProductCard'
import { useAuth } from '../context/AuthContext'
import { productService } from '../services/productService'
import type { Product } from '../types/product'

type AvailabilityFilter = 'all' | 'in-stock' | 'out-of-stock'

type Feedback = {
  type: 'success' | 'danger'
  message: string
}

const ProductCatalogPage = () => {
  const { session } = useAuth()
  const [products, setProducts] = useState<Product[]>([])
  const [searchTerm, setSearchTerm] = useState('')
  const [availability, setAvailability] = useState<AvailabilityFilter>('all')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [feedback, setFeedback] = useState<Feedback | null>(null)
  const [addingProductId, setAddingProductId] = useState<string | null>(null)

  useEffect(() => {
    let active = true

    const loadProducts = async () => {
      setLoading(true)
      setError(null)
      try {
        const items = await productService.getProducts()
        if (active) {
          setProducts(items)
        }
      } catch (loadError) {
        if (active) {
          setError((loadError as Error).message || 'Unable to load products right now.')
        }
      } finally {
        if (active) {
          setLoading(false)
        }
      }
    }

    void loadProducts()

    return () => {
      active = false
    }
  }, [])

  const filteredProducts = useMemo(() => {
    const term = searchTerm.trim().toLowerCase()
    return products.filter((product) => {
      const matchesSearch =
        !term ||
        product.name.toLowerCase().includes(term) ||
        product.sku.toLowerCase().includes(term) ||
        (product.description?.toLowerCase().includes(term) ?? false)

      if (!matchesSearch) {
        return false
      }

      if (availability === 'in-stock') {
        return product.inStock
      }

      if (availability === 'out-of-stock') {
        return !product.inStock
      }

      return true
    })
  }, [availability, products, searchTerm])

  const handleAddToCart = async (product: Product) => {
    setAddingProductId(product.id)
    setFeedback(null)
    try {
      await productService.addToCart(product.id, 1)
      setFeedback({ type: 'success', message: `${product.name} was added to your cart.` })
    } catch (cartError) {
      setFeedback({
        type: 'danger',
        message: (cartError as Error).message || 'Unable to add this item to your cart right now.',
      })
    } finally {
      setAddingProductId(null)
    }
  }

  return (
    <div className="container py-4">
      <div className="d-flex flex-column flex-lg-row justify-content-between align-items-lg-center mb-4 gap-3">
        <div>
          <h1 className="h3 mb-1">Product catalog</h1>
          <p className="text-muted mb-0">Browse inventory and add items directly to the sales cart.</p>
        </div>
        {session?.user && (
          <div className="text-muted small">
            Logged in as <strong>{session.user.name ?? session.user.email}</strong>
          </div>
        )}
      </div>

      <div className="row g-3 align-items-end mb-4">
        <div className="col-12 col-md-6 col-lg-5">
          <label htmlFor="product-search" className="form-label">
            Search
          </label>
          <input
            id="product-search"
            type="search"
            className="form-control"
            placeholder="Search by name, SKU or description"
            value={searchTerm}
            onChange={(event) => setSearchTerm(event.target.value)}
          />
        </div>
        <div className="col-12 col-md-3 col-lg-3">
          <label htmlFor="availability-filter" className="form-label">
            Availability
          </label>
          <select
            id="availability-filter"
            className="form-select"
            value={availability}
            onChange={(event) => setAvailability(event.target.value as AvailabilityFilter)}
          >
            <option value="all">All products</option>
            <option value="in-stock">In stock</option>
            <option value="out-of-stock">Out of stock</option>
          </select>
        </div>
      </div>

      {feedback && (
        <div className={`alert alert-${feedback.type}`} role="status">
          {feedback.message}
        </div>
      )}

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      {loading ? (
        <div className="d-flex justify-content-center py-5" role="status" aria-live="polite">
          <div className="spinner-border text-primary" role="status" aria-hidden="true" />
          <span className="visually-hidden">Loading products…</span>
        </div>
      ) : (
        <>
          {filteredProducts.length === 0 ? (
            <div className="text-center py-5 text-muted">No products match your search just yet.</div>
          ) : (
            <div className="row g-4">
              {filteredProducts.map((product) => (
                <div className="col-12 col-sm-6 col-lg-4" key={product.id}>
                  <ProductCard
                    product={product}
                    onAddToCart={() => handleAddToCart(product)}
                  />
                </div>
              ))}
            </div>
          )}
        </>
      )}

      {addingProductId && (
        <div className="toast-container position-fixed bottom-0 end-0 p-3">
          <div className="toast show align-items-center text-bg-primary border-0" role="status">
            <div className="d-flex">
              <div className="toast-body">Adding item to cart…</div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default ProductCatalogPage
