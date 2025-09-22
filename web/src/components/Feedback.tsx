import { ReactNode } from 'react'

export const LoadingState = ({ message = 'Loading data…' }: { message?: string }) => (
  <div role="status" className="feedback loading">
    {message}
  </div>
)

export const ErrorState = ({ message, retry }: { message: string; retry?: () => void }) => (
  <div role="alert" className="feedback error">
    <p>{message}</p>
    {retry ? (
      <button type="button" className="secondary" onClick={retry}>
        Try again
      </button>
    ) : null}
  </div>
)

export const EmptyState = ({ title, description, action }: { title: string; description: string; action?: ReactNode }) => (
  <div className="feedback empty">
    <h3>{title}</h3>
    <p>{description}</p>
    {action}
  </div>
)
