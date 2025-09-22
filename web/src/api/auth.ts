import { apiRequest, setAuthToken } from './client'

export interface LoginRequest {
  username: string
  password: string
}

export interface AuthenticatedUser {
  id: string
  name: string
  email?: string
  roles?: string[]
}

export interface LoginResponse {
  token: string
  user?: AuthenticatedUser
}

export const login = async (credentials: LoginRequest) => {
  const response = await apiRequest<LoginResponse>('/auth/login', {
    method: 'POST',
    body: credentials,
    auth: false,
  })

  setAuthToken(response.token)

  return response
}
