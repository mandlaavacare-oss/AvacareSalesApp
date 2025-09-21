# AGENTS

## Project scope & SDK reference
- This repository hosts the **Avacare Sales Application** that integrates with the [Sage Pastel Evolution C# SDK](https://developmentzone.pastel.co.za/index.php?title=User_Guide).
- Core business areas we support map to the SDK modules:
  - Transactions → Accounts Payable, Accounts Receivable, General Ledger, Inventory, Order Entry.
  - Contact Management Incidents, Job Costing, and Additional Functionality.
- All new code that touches the SDK must go through dedicated wrapper services—UI, controllers, and higher-level business services must never call the SDK directly.

## Repository layout conventions
- Place application code inside `src/`, grouped by domain:
  - `src/Transactions/AccountsReceivable`, `src/Transactions/OrderEntry`, etc.
  - Within each domain folder, organise code into `Adapters/`, `Models/`, `Services/`, and `Controllers/` (or UI).
- Put automated tests in `tests/`, mirroring the structure of `src/`.
- Keep documentation in `docs/`:
  - Functional specs and SDK notes in `docs/transactions/<module>.md`.
  - Scenario guides in `docs/scenarios/`.
- Solution/project files should live at the repo root (`AvacareSalesApp.sln`, `*.csproj`) or inside their respective domain folders when multiple projects exist.

## Coding & architectural guidelines
- Follow standard .NET conventions: PascalCase for types, camelCase for locals/parameters, suffix async methods with `Async`, and enable nullable reference types.
- Use dependency injection for all SDK clients and configuration providers; never hardcode credentials or environment-specific values.
- Enforce separation of concerns:
  - **Adapters** translate between SDK models and internal domain models.
  - **Services** contain business workflows and validations.
  - **Controllers/UI** orchestrate requests and responses only.
- Wrap SDK exceptions into domain-specific errors, surface user-friendly messages, and log diagnostic details.
- Do not place try/catch blocks around `using` directives or imports.

## Module-specific expectations
- Each SDK module must expose a clear entry-point service (e.g., `AccountsReceivableService`, `OrderEntryQuoteService`).
- Document required validations and workflows per module inside `docs/transactions/<module>.md`.
- When adding a new SDK wrapper:
  - Provide an integration note describing the mapped SDK calls and assumptions.
  - Include at least one integration-style test (mocked if necessary) that proves the request/response mapping.

## Testing & quality gates
- All code changes must be covered by unit tests; run `dotnet test` before committing.
- Keep formatting consistent by running `dotnet format` (or the configured formatter) when C# files change.
- If integration tests depend on sandbox credentials, store secrets in `appsettings.Development.json` or environment variables that are excluded from version control.
- Document any additional checks required for specific modules inside their scoped `AGENTS.md` files when present.

## Documentation & examples
- Update the top-level `README.md` when introducing a new module, domain, or major feature.
- Maintain scenario-driven guides in `docs/scenarios/` (e.g., “Convert quote to sales order”, “Create job costing incident”).
- Keep SDK usage examples current with the SDK version referenced by the repository.

## Contribution workflow
- Work directly on the `main` branch unless repository policies dictate otherwise; do not create additional branches within this environment.
- Use descriptive commit messages following the pattern `scope: summary` (e.g., `order-entry: add quote creation flow`).
- Every change must include updated tests and documentation where applicable.
- Ensure the working tree is clean and all tests pass before final submission.

## PR / Review guidance
- Summaries must describe business capabilities impacted (e.g., “Add Accounts Receivable invoice posting service”).
- List the exact commands executed for testing in the PR body, marking pass/fail status.
- Call out any new configuration, secrets, or external dependencies introduced by the change.
- Flag follow-up work or known limitations explicitly in the PR notes section.
