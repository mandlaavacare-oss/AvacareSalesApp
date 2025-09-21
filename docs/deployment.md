# Deployment Guide

This guide covers the two supported hosting targets for the Avacare Sales Application and details the steps required to promote a build that has passed automated validation. Always ensure the repository is on the commit you intend to deploy before following either path.

## Common validation steps

Run the following commands from the repository root before building artifacts for any environment. The deployment must be blocked until both commands succeed.

```bash
dotnet test          # Verifies the API and shared libraries
dotnet publish src/Server/Server.csproj -c Release -o build/publish --no-build
npm run build        # Produces the optimized frontend bundle under web/dist
```

> **Note:** `dotnet publish` is invoked with `--no-build` after `dotnet test` to reuse the compiled binaries. If you skip the preceding commands, drop `--no-build` so the publish step can compile the solution.

## IIS deployment (Windows Server)

### Prerequisites

- Windows Server 2019 or later with the **Web Server (IIS)** role installed.
- IIS features: **Web Management Service**, **ASP.NET Core Module**, and **Static Content**.
- [.NET 8.0 Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.0-windows-hosting-bundle-installer) installed on the server.
- .NET SDK 8.0 (for building on the deployment workstation): verify with `dotnet --list-sdks`.
- Node.js 18 LTS or later (for building the frontend bundle): verify with `node --version`.
- Optional: Web Deploy 4.0 if you plan to push the package directly to IIS.

### Create a publish profile

1. From a Windows workstation with Visual Studio or the .NET SDK installed, run:
   ```bash
   dotnet publish src/Server/Server.csproj -c Release -o build/publish
   ```
2. Copy the generated `build/publish` directory to a shared location.
3. If you use Visual Studio, create a **FolderProfile** publish profile that targets the same output directory. This profile can be exported (`*.pubxml`) and reused by build pipelines.

### Prepare `web.config`

Include or update a `web.config` file in the publish output to configure the ASP.NET Core Module and environment variables used by the application.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" arguments="Server.dll" stdoutLogEnabled="false" hostingModel="InProcess">
      <environmentVariables>
        <add name="ASPNETCORE_ENVIRONMENT" value="Production" />
        <add name="ConnectionStrings__Default" value="Server=sql.example.com;Database=AvacareSalesApp;User Id=...;Password=...;TrustServerCertificate=True" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

Replace the placeholder connection string with the production SQL Server endpoint and credentials. Add any additional configuration keys required by the Sage integration (for example, `SAGE__CompanyCode` or `Jwt__Authority`).

### Configure IIS and deploy

1. In IIS Manager, create a new **Application Pool**:
   - .NET CLR version: **No Managed Code**.
   - Managed pipeline mode: **Integrated**.
   - Enable **Start application pool immediately**.
2. Create a new **Website** or Application pointing to `C:\inetpub\wwwroot\AvacareSalesApp` (or your preferred path) and assign the application pool created above.
3. Copy the contents of `build/publish` (including `web.config`) to the IIS site directory. If you are using Web Deploy, run a command such as:
   ```bash
   "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe" -verb:sync -source:contentPath="build/publish" -dest:contentPath="AvacareSalesApp",computerName="https://server:8172/msdeploy.axd",userName="deployUser",password="*****",authType=Basic
   ```
4. Grant the IIS application pool identity read access to the deployment directory and permissions to any external resources (file shares, certificates) required by the SDK wrappers.
5. Recycle the application pool and browse to the site URL to confirm the API responds under `/swagger` and that the React bundle is served correctly.

### Environment-specific configuration

- Store secrets (API keys, Sage credentials) outside of source control and inject them via `web.config` or using the **Configuration Editor** in IIS.
- If the frontend bundle requires runtime configuration, ensure the static files in `web/dist` include the correct `base` URLs before copying them to the server.

## Azure App Service deployment

### Prerequisites

- An Azure subscription with permission to create App Service plans, web apps, and deployment slots.
- Azure CLI 2.45.0 or later installed locally: verify with `az version`.
- Logged into Azure CLI (`az login`) and set the correct subscription (`az account set --subscription <SUBSCRIPTION_ID>`).
- .NET SDK 8.0 and Node.js 18+ on the build agent (verify with `dotnet --list-sdks` and `node --version`).

### Build artifacts

1. Run the validation commands described in [Common validation steps](#common-validation-steps):
   ```bash
   dotnet test
   npm run build
   dotnet publish src/Server/Server.csproj -c Release -o build/publish
   ```
2. Ensure the frontend build output (`web/dist`) is copied into the publish folder if it must be served by the API. If the frontend is hosted separately (e.g., Azure Static Web Apps), deploy it following that service's guidance.

### Provision Azure resources

```bash
RESOURCE_GROUP="avacare-sales-rg"
PLAN_NAME="avacare-sales-plan"
WEBAPP_NAME="avacare-sales-api"
LOCATION="westeurope"

az group create --name $RESOURCE_GROUP --location $LOCATION
az appservice plan create --name $PLAN_NAME --resource-group $RESOURCE_GROUP --sku P1v3 --is-linux false
az webapp create --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP --plan $PLAN_NAME --runtime "DOTNET|8.0"
```

If you need a staging slot:

```bash
az webapp deployment slot create --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP --slot staging
```

### Configure application settings and connection strings

```bash
az webapp config appsettings set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings ASPNETCORE_ENVIRONMENT=Production \
             WEBSITES_PORT=8080 \
             SAGE__CompanyCode="ACME" \
             Jwt__Authority="https://identity.example.com"

az webapp config connection-string set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --connection-string-type SQLAzure \
  --settings Default="Server=tcp:sql-prod.database.windows.net,1433;Initial Catalog=AvacareSalesApp;User ID=...;Password=...;Encrypt=True;TrustServerCertificate=False"
```

Repeat the commands with `--slot staging` to configure deployment slots.

### Deploy build output

Deploy a ZIP package that contains the `build/publish` directory contents:

```bash
cd build/publish
zip -r ../server.zip .
cd ..
az webapp deploy --resource-group $RESOURCE_GROUP --name $WEBAPP_NAME --src-path server.zip --type zip
```

If you prefer continuous deployment from source control, configure it with:

```bash
az webapp deployment source config \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --repo-url https://github.com/Avacare/AvacareSalesApp \
  --branch main \
  --manual-integration
```

Add a `.deployment` or `azuredeploy.sh` script at the repository root if you need custom build commands. App Service will execute `dotnet publish` and `npm run build` automatically when `SCM_DO_BUILD_DURING_DEPLOYMENT=true` is set via `az webapp config appsettings set`.

### Manage deployment slots

- Deploy new builds to the `staging` slot by passing `--slot staging` to `az webapp deploy`.
- Smoke test the API and frontend at `https://<WEBAPP_NAME>-staging.azurewebsites.net`.
- Swap staging to production once validated:
  ```bash
  az webapp deployment slot swap --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP --slot staging --target-slot production
  ```
- Roll back by swapping again if issues are detected.

### Post-deployment validation

- Check application logs using `az webapp log tail --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP`.
- Confirm connection strings and environment variables using `az webapp config appsettings list` and `az webapp config connection-string list`.
- Monitor the health of the slots and configure autoscale or alert rules as needed.

---

Keep this document alongside the repository so any engineer can reproduce the deployment process. Update the guide when infrastructure or configuration requirements change.
