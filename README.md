# AvacareSalesApp

Central hub for the Avacare Sales Application built on top of the Sage Pastel Evolution C# SDK.

## Documentation
- [Sage Evolution SDK (C# Transactions) Technical User Guide](docs/transactions/sage-evolution-sdk-transactions.md)

## Frontend (WebClient)

The React-based customer-facing experience lives under [`src/WebClient`](src/WebClient). To work on the interface:

1. Install prerequisites (Node.js 20+, npm 10+).
2. Install dependencies and run the development server:

   ```bash
   cd src/WebClient
   npm install
   npm run dev
   ```

3. Provide the backend API base URL via `VITE_API_BASE_URL` in an `.env.local` file when the API is available.
4. Run the automated Vitest suite with `npm test` and produce a production build via `npm run build` when preparing a release.
