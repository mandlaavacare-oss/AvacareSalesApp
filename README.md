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

#### Branch configuration file

Online branch accounting requires the API to know which Sage branch should back each request. Place a `branchConfiguration.json` file in the content root (next to `appsettings.json`) and add it to the configuration pipeline (for example with `builder.Configuration.AddJsonFile("branchConfiguration.json", optional: false, reloadOnChange: true)`) so edits are picked up without restarting the site. A minimal example looks like this:

```json
{
  "branches": [
    {
      "code": "JHB",
      "pastelBranchId": 1,
      "description": "Johannesburg head office"
    },
    {
      "code": "CPT",
      "pastelBranchId": 4,
      "description": "Cape Town warehouse"
    }
  ]
}
```

At runtime the controller (or middleware) resolves the branch code supplied by the caller, locates the matching entry in the configuration, and then executes the Sage SDK call sequence before touching transactional data:

```csharp
var branch = _branchResolver.Resolve("JHB");
DatabaseContext.SetBranchContext(branch.PastelBranchId);
DatabaseContext.BeginTran();
// Invoke the appropriate adapter/service logic
DatabaseContext.CommitTran();
```

When an error occurs after the branch context is set, the surrounding transaction wrapper should call `DatabaseContext.RollbackTran()` instead of `CommitTran()`.

#### Supplying secrets via environment variables or user secrets

The application uses standard ASP.NET Core configuration precedence. Values in `appsettings.json` and `appsettings.{Environment}.json` are considered defaults; environment variables and the development user secrets store take priority and should be used for sensitive data. Use the following key shapes—`ConnectionStrings:Default` is read today, and the Sage entries illustrate how additional secrets should be named once the SDK adapters are implemented:

| Purpose | Configuration key | Environment variable form |
| --- | --- | --- |
| SQL connection string | `ConnectionStrings:Default` | `ConnectionStrings__Default` |
| Sage agent username | `Sage:Agent` | `Sage__Agent` |
| Sage agent password | `Sage:Password` | `Sage__Password` |
| Optional API keys (per adapter) | `Sage:ApiKeys:<Name>` | `Sage__ApiKeys__<Name>` |

For local development, initialise user secrets once with `dotnet user-secrets init` from the `src/Server` directory and then set values, for example:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=.;Database=AvacareSalesApp;User Id=..."
dotnet user-secrets set "Sage:Agent" "integration.bot"
dotnet user-secrets set "Sage:Password" "<strong password>"
```

When the site starts it loads values in the following order (later entries override earlier ones): `appsettings.json` → `appsettings.{Environment}.json` → user secrets (only in Development) → environment variables. This allows you to keep non-sensitive defaults in source control while providing production credentials through secure stores.

#### Deployment-time configuration and secret management

All configuration files (`appsettings*.json` and `branchConfiguration.json`) are watched for changes and automatically reloaded, but deployment environments typically handle secrets differently:

- **IIS / Windows Hosting** – use `web.config` transforms to point at production-only JSON files or to set environment variables (e.g., `ConnectionStrings__Default`) during the publish step.
- **Azure App Service** – set application settings for each key (including `ConnectionStrings__Default`, `Sage__Agent`, etc.). App Service injects them as environment variables and restarts the worker to apply updates; no code changes are required.
- **Containers / Kubernetes** – project secrets as environment variables using Docker `-e` flags, Kubernetes `Secret` resources, or Azure Key Vault references. Mount `branchConfiguration.json` through a ConfigMap or bind mount when branch mappings need to change without redeployment.

Remember to update DevOps pipelines or infrastructure-as-code definitions whenever new keys are introduced so that sensitive values remain outside the repository.

## Container deployment notes

- **API container** – publishes the ASP.NET Core API to `/app/publish` and listens on port `8080`. When deploying to Azure App Service for Containers, set `WEBSITES_PORT=8080` (or the equivalent configuration variable) so the platform forwards traffic correctly.
- **Web container** – serves the static frontend bundle from NGINX on port `80`, ready to sit behind an ingress controller or Application Gateway without additional port remapping.
