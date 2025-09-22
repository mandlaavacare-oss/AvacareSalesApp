import '@testing-library/jest-dom/vitest'
import { afterEach, beforeEach } from 'vitest'

const originalFetch = globalThis.fetch

beforeEach(() => {
  globalThis.fetch = originalFetch
})

afterEach(() => {
  globalThis.fetch = originalFetch
})
