# Avacare Sales Web

This React application provides the browser-based console for the Avacare Sales platform. It integrates with the Avacare API to
cover authentication, customer/product lookups, order entry, invoicing, and payment allocation workflows.

## Getting started

```bash
npm install
npm run dev
```

The development server expects the Avacare API to be available on the same origin (or proxied) and to expose the following
endpoints:

- `POST /auth/login`
- `GET /customers`
- `GET /products`
- `GET /orders`
- `POST /orders`
- `GET /invoices`
- `GET /payments`
- `POST /payments`

You can override the API base URL by setting `VITE_API_BASE_URL`.

## Quality checks

- `npm run lint` – static analysis using ESLint.
- `npm run test` – Vitest + Testing Library integration tests covering authentication and order entry flows.

## Application areas

| Area        | Description                                                                 |
|-------------|-----------------------------------------------------------------------------|
| Auth        | Username/password authentication, token persistence, and logout handling.   |
| Customers   | Read-only directory displaying account numbers, balances, and contacts.     |
| Products    | Catalogue view with pricing and stock visibility for order capture.         |
| Orders      | Guided capture flow with customer/product selection and recent order list.  |
| Invoices    | Summary of accounts receivable invoices with status and balances.           |
| Payments    | Capture of customer payments with invoice allocation and payment history.   |

