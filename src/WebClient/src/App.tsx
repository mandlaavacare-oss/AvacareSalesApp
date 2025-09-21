import { Navigate, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import ProtectedRoute from './components/ProtectedRoute'
import { AuthProvider } from './context/AuthContext'
import LoginPage from './pages/LoginPage'
import ProductCatalogPage from './pages/ProductCatalogPage'

const App = () => {
  return (
    <AuthProvider>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/catalog"
            element={
              <ProtectedRoute>
                <ProductCatalogPage />
              </ProtectedRoute>
            }
          />
          <Route path="*" element={<Navigate to="/catalog" replace />} />
        </Route>
      </Routes>
    </AuthProvider>
  )
}

export default App
