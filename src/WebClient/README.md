# Avacare Sales Web Client

This project hosts the React front-end for the Avacare Sales application. It was bootstrapped with [Vite](https://vitejs.dev) and uses TypeScript, React Router, and Bootstrap 5 for a responsive experience.

## Getting started

### Prerequisites

- Node.js 20+
- npm 10+

### Installation

```bash
cd src/WebClient
npm install
```

### Local development

```bash
npm run dev
```

The development server runs on [http://localhost:5173](http://localhost:5173) by default. Environment variables defined in a `.env.local` file prefixed with `VITE_` are available to the client. Set `VITE_API_BASE_URL` to point to the backend API once it is available:

```
VITE_API_BASE_URL=https://localhost:5001/api
```

### Testing

```bash
npm test
```

Tests run with [Vitest](https://vitest.dev) and [React Testing Library](https://testing-library.com/docs/react-testing-library/intro). They cover authentication flows and catalogue interactions.

### Production build

```bash
npm run build
```

Static assets are emitted to `dist/` and can be deployed to any static host. Use `npm run preview` to verify the build locally.

## Project structure

```
src/WebClient/
├── src/
│   ├── api/              # HTTP client helpers
│   ├── components/       # Shared UI building blocks
│   ├── context/          # React context providers
│   ├── pages/            # Route-backed screens (login, product catalogue)
│   ├── services/         # API-facing domain services
│   ├── types/            # Shared TypeScript contracts
│   └── setupTests.ts     # Vitest configuration
├── public/               # Static assets
├── package.json          # Scripts and dependencies
└── vite.config.ts        # Vite + Vitest configuration
```

## Key features

- **Token-based authentication** – credentials are exchanged for a bearer token that is stored in `sessionStorage` for the active browser session.
- **Product catalogue** – responsive grid with search and availability filters, price and stock visibility, and an add-to-cart action that calls the cart API when available.
- **Shared layout** – navigation shell with authenticated state awareness and responsive Bootstrap styling.
- **API client abstraction** – reusable HTTP client that injects the session token and gracefully handles JSON error responses.

Update the backend endpoints referenced in `src/services` once the APIs are available. The services currently fall back to sample data so the UI remains usable during early development.
