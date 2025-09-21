import { useState } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const Layout = () => {
  const { isAuthenticated, logout, session } = useAuth()
  const [isNavOpen, setIsNavOpen] = useState(false)
  const navigate = useNavigate()

  const handleToggle = () => setIsNavOpen((previous) => !previous)

  const handleLogout = () => {
    logout()
    closeNav()
    navigate('/login')
  }

  const closeNav = () => setIsNavOpen(false)

  return (
    <div className="d-flex flex-column min-vh-100">
      <header>
        <nav className="navbar navbar-expand-lg navbar-dark bg-primary shadow-sm">
          <div className="container">
            <Link className="navbar-brand text-uppercase text-brand" to="/catalog" onClick={closeNav}>
              Avacare Sales
            </Link>
            <button
              className="navbar-toggler"
              type="button"
              aria-controls="main-navigation"
              aria-expanded={isNavOpen}
              aria-label="Toggle navigation"
              onClick={handleToggle}
            >
              <span className="navbar-toggler-icon" />
            </button>
            <div className={`collapse navbar-collapse${isNavOpen ? ' show' : ''}`} id="main-navigation">
              <ul className="navbar-nav me-auto mb-2 mb-lg-0">
                <li className="nav-item">
                  <NavLink
                    className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
                    to="/catalog"
                    onClick={closeNav}
                  >
                    Product Catalog
                  </NavLink>
                </li>
              </ul>
              <div className="d-flex align-items-center gap-3">
                {isAuthenticated ? (
                  <>
                    <span className="text-white-50 small">
                      Signed in as <strong>{session?.user.name ?? session?.user.email}</strong>
                    </span>
                    <button className="btn btn-outline-light btn-sm" type="button" onClick={handleLogout}>
                      Log out
                    </button>
                  </>
                ) : (
                  <Link className="btn btn-outline-light btn-sm" to="/login" onClick={closeNav}>
                    Log in
                  </Link>
                )}
              </div>
            </div>
          </div>
        </nav>
      </header>
      <main>
        <Outlet />
      </main>
      <footer className="bg-dark text-light text-center py-3 mt-auto">
        <small>&copy; {new Date().getFullYear()} Avacare Sales. All rights reserved.</small>
      </footer>
    </div>
  )
}

export default Layout
