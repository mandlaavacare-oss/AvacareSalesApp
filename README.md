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

## Backend server

The repository now includes an ASP.NET Core Web API that provides authenticated access to core Sage modules such as Accounts Receivable, Inventory, and Order Entry. Business logic remains encapsulated inside domain services that wrap Sage SDK adapters as mandated in the project guidelines.

### Prerequisites

- .NET SDK 8.0 or later

### Project layout

```
src/Server/                 # ASP.NET Core Web API project
  Controllers/              # HTTP endpoints orchestrating service calls
  Infrastructure/           # Cross-cutting concerns (auth, database context)
  Transactions/             # Domain-specific adapters, models, services
tests/Server.Tests/         # Unit tests for services and controllers
```

### Running the API locally

```
dotnet restore               # Restore all solution dependencies
dotnet build                 # Compile the solution
dotnet run --project src/Server/Server.csproj
```

The API starts on `https://localhost:5001` (or the port reported by the CLI). Swagger UI is available in development mode at `/swagger` to aid manual exploration of endpoints such as:

- `POST /auth/login`
- `GET /customers/{id}`
- `GET /products`
- `POST /orders`
- `POST /invoices`
- `POST /payments`

Each controller coordinates its domain service through the shared `IDatabaseContext`, guaranteeing that Sage SDK operations execute inside a transaction via `BeginTran`, `CommitTran`, and `RollbackTran` boundaries.

### Testing

Unit tests validate both the service layer (ensuring Sage adapters are invoked correctly) and the controller layer (verifying transaction management and HTTP responses). Run them with:

```
dotnet test
```

### Configuration

Runtime integration with the Sage SDK requires concrete adapter implementations. The default `Sage*Adapter` classes are placeholders that must be completed with real SDK calls and configuration (e.g., connection strings, credentials) before production deployment.
