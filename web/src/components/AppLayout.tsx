import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const navigation = [
  { path: '/customers', label: 'Customers' },
  { path: '/products', label: 'Products' },
  { path: '/orders', label: 'Order Entry' },
  { path: '/invoices', label: 'Invoices' },
  { path: '/payments', label: 'Payments' },
]

export const AppLayout = () => {
  const { logout, user } = useAuth()

  return (
    <div className="app-shell">
      <header className="app-header">
        <div>
          <h1>Avacare Sales</h1>
          <p className="tagline">Sales operations for Sage Pastel Evolution</p>
        </div>
        <div className="user-actions">
          {user ? <span className="user-name">{user.name}</span> : null}
          <button className="secondary" type="button" onClick={logout}>
            Log out
          </button>
        </div>
      </header>
      <div className="app-body">
        <nav className="sidebar">
          <ul>
            {navigation.map((item) => (
              <li key={item.path}>
                <NavLink to={item.path} className={({ isActive }) => (isActive ? 'active' : '')}>
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
