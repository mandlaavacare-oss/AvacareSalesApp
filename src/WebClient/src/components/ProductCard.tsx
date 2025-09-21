import type { Product } from '../types/product'

interface ProductCardProps {
  product: Product
  onAddToCart: () => void
}

const formatCurrency = (value: number, currency: string) =>
  new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(value)

const ProductCard = ({ product, onAddToCart }: ProductCardProps) => {
  return (
    <div className="card h-100 shadow-sm">
      <div className="card-body d-flex flex-column">
        <div className="d-flex justify-content-between align-items-start mb-3">
          <div>
            <h5 className="card-title mb-1">{product.name}</h5>
            <p className="text-muted small mb-0">SKU: {product.sku}</p>
          </div>
          <span className={`badge ${product.inStock ? 'text-bg-success' : 'text-bg-secondary'}`}>
            {product.inStock ? 'In stock' : 'Out of stock'}
          </span>
        </div>
        {product.description && <p className="card-text flex-grow-1">{product.description}</p>}
        <div className="mt-3">
          <p className="fw-bold fs-5 mb-2">{formatCurrency(product.price, product.currency)}</p>
          {typeof product.stockQuantity === 'number' && (
            <p className="text-muted small mb-3">{product.stockQuantity} units available</p>
          )}
          <button
            type="button"
            className="btn btn-primary w-100"
            onClick={onAddToCart}
            disabled={!product.inStock}
          >
            Add to cart
          </button>
        </div>
      </div>
    </div>
  )
}

export default ProductCard
