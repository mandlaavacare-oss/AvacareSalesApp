# AvacareSalesApp

Central hub for the Avacare Sales Application built on top of the Sage Pastel Evolution C# SDK.

## Frontend workspace
The Node/React frontend lives under `web/` and is managed as an npm workspace.

- `web/src/` contains the React application source written in TypeScript.
- `web/public/` holds static assets copied as-is to the final build output.
- `web/index.html` is the Vite entry document that loads the bundle generated from `src/main.tsx`.
- `web/vite.config.ts` and `web/tsconfig*.json` define the bundler and TypeScript compiler settings used by the build script.

Run `npm install` followed by `npm run build` from the repository root to compile the frontend bundle.

## Documentation
- [Sage Evolution SDK (C# Transactions) Technical User Guide](docs/transactions/sage-evolution-sdk-transactions.md)
