export interface UserProfile {
  id: string
  email: string
  name?: string
}

export interface AuthSession {
  token: string
  refreshToken?: string
  user: UserProfile
}

export interface LoginCredentials {
  email: string
  password: string
}
