# OpenIddict.Management.Core

[![NuGet](https://img.shields.io/nuget/v/Mingoll.OpenIddict.Management.Core.svg?style=flat-square&label=NuGet)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Core)
[![Downloads](https://img.shields.io/nuget/dt/Mingoll.OpenIddict.Management.Core.svg?style=flat-square)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Core)
[![Target Frameworks](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

`OpenIddict.Management.Core` (`Mingoll.OpenIddict.Management.Core`) defines the foundational domain abstractions, service contracts, login and token issuance orchestration pipelines, audit trail models, full configuration export/import engine (with dynamic assembly versioning), bulk operations, unified result models, and background token cleanup scheduling for the OpenIddict Management suite.

Install this package when building headless identity services, crafting custom storage adapters (e.g., Dapper, MongoDB, Cosmos DB), or implementing application-specific user authentication and management pipelines without coupling your domain to ASP.NET Core UI or Entity Framework Core.

---

## Installation

Install via the .NET CLI:

```bash
dotnet add package Mingoll.OpenIddict.Management.Core
```

Or via the Visual Studio Package Manager Console:

```powershell
Install-Package Mingoll.OpenIddict.Management.Core
```

---

## Prerequisites & Dependencies

### Supported Frameworks
- **.NET 8.0** (LTS)
- **.NET 9.0** (STS)
- **.NET 10.0**

### Required Package Dependencies
- `OpenIddict.Abstractions` (>= 6.x / 7.x)
- `OpenIddict.Server` (>= 6.x / 7.x)
- `Microsoft.Extensions.Options`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Hosting.Abstractions`

### Companion Packages
- For relational database persistence: install [`Mingoll.OpenIddict.Management.Storage.EfCore`](../OpenIddict.Management.Storage.EfCore/README.md).
- For pre-built Minimal API endpoints: install [`Mingoll.OpenIddict.Management.Endpoints`](../OpenIddict.Management.Endpoints/README.md).
- For an embedded admin UI: install [`Mingoll.OpenIddict.Management.Dashboard`](../OpenIddict.Management.Dashboard/README.md).

---

## Core Features & Architecture

- **Application & Scope Management Contracts (`IApplicationManagementService`, `IScopeManagementService`)**:
  - Full CRUD operations with rich metadata (`ApplicationStatus`, `ApplicationEnvironment`, tags, allowed roles, permissions catalog).
  - Client secret generation and rotation with instant reveal.
  - Soft and hard deletion support.
  - Batch operations: `BulkCreateAsync`, `BulkDeleteAsync`, and `SetStatusByEnvironmentAsync`.
- **Full Configuration Export & Import (`IConfigurationExportImportService`)**:
  - Export and import applications and scopes.
  - Export and import complete OpenIddict Server runtime options (`OpenIddictServerOptions`) and Management settings (`OpenIddictManagementOptions`, `TokenCleanupOptions`).
  - Container package `ManagementExportPackage` with dynamic **`VersionPrefix`** resolution from assembly metadata (e.g., `1.1.0`).
  - Granular import options (`OverwriteExisting`, `ImportApplications`, `ImportScopes`, `ImportConfigurations`).
- **Audit Trail & Logging (`ManagementAuditEntry`, `IAuditTrailStore`)**:
  - Captures security and administrative events (`Category`, `Action`, `EntityId`, `EntityName`, `Actor`, `Details`, `Success`, `ErrorMessage`, `Timestamp`).
  - Automatic event dispatching via `AuditTrailEventHandler` listening to domain events.
  - Built-in `InMemoryAuditTrailStore` and toggle via `OpenIddictManagementOptions.EnableAuditLogging`.
- **Login Orchestration & Token Principal Building (`IOpenIddictLoginEngine`, `IOpenIddictTokenService`)**:
  - Validates credentials via custom `IUserAuthenticationProvider`.
  - Constructs `ClaimsPrincipal` with destination routing (`AccessToken`, `IdentityToken`, `AccessTokenAndIdentityToken`).
- **Background Token Pruning & Management (`ITokenCleanupJobManager`)**:
  - Scheduled background pruning of expired and revoked tokens.
  - Runtime adjustments of interval, batch size, and manual trigger (`RunCleanupNowAsync`).
- **Application Status & Environment Validation (`IOpenIddictValidationHandler<>`, `IOpenIddictServerHandler<>`, `IOpenIddictApplicationValidator`)**:
  - Automatically intercepts login and token requests across **any flow** (`authorization_code`, `client_credentials`, `password`, `refresh_token`, `device_code`) and the interactive `/connect/authorize` endpoint.
  - Enforces access token validation in OpenIddict.Validation via `IOpenIddictValidationHandler<ProcessAuthenticationContext>`.
  - Configurable via `.AddApplicationValidation(options => ...)` to enforce operational statuses (`Active`, `Disabled`, `Deleted`), deployment environments (`Development`, `Staging`, `Production`), and custom developer validation rules (`ValidateCustom`).

---

## Configuration & Setup

### Service Registration (`Program.cs`)

```csharp
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Extensions;
using OpenIddict.Management.Models;
using OpenIddict.Management.Options;

var builder = WebApplication.CreateBuilder(args);

// Register Core Management services with options
builder.Services.AddOpenIddictManagement(options =>
{
    options.RoutePrefix = "/api/management";
    options.RequireHttps = true;
    options.EnableAuditLogging = true; // Toggle audit logging
})
.AddAuthenticationProvider<AppUserAuthenticationProvider>()
.AddApplicationValidation(options =>
{
    options.RequireStatus(ApplicationStatus.Active);
    options.RequireEnvironments(ApplicationEnvironment.Production, ApplicationEnvironment.Staging);
})
.AddTokenCleanup(options =>
{
    options.IsEnabled = true;
    options.Interval = TimeSpan.FromHours(12);
    options.BatchSize = 250;
    options.IncludeRevoked = true;
});
```

### Configuration via `appsettings.json`

```json
{
  "OpenIddictManagement": {
    "RoutePrefix": "/api/management",
    "RequireHttps": true,
    "EnableAuditLogging": true
  },
  "TokenCleanup": {
    "IsEnabled": true,
    "BatchSize": 250,
    "Interval": "12:00:00",
    "IncludeRevoked": true
  }
}
```

Bind the configuration sections in `Program.cs`:

```csharp
builder.Services.Configure<OpenIddictManagementOptions>(
    builder.Configuration.GetSection("OpenIddictManagement"));

builder.Services.Configure<TokenCleanupOptions>(
    builder.Configuration.GetSection("TokenCleanup"));

builder.Services.AddOpenIddictManagement()
    .AddAuthenticationProvider<AppUserAuthenticationProvider>();
```

---

## Usage Examples

### 1. Application Management & Bulk Operations

```csharp
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;

public class ApplicationAdministration(IApplicationManagementService appService)
{
    public async Task ManageApplicationsAsync(CancellationToken cancellationToken)
    {
        // 1. Create client application
        var createResult = await appService.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "mobile-portal",
            DisplayName = "Mobile Portal App",
            ClientType = "confidential",
            Environment = ApplicationEnvironment.Production,
            Permissions = ["ept:token", "ept:authorization", "gt:authorization_code", "gt:refresh_token", "scp:openid", "scp:profile"],
            RedirectUris = ["https://mobile.example.com/callback"],
            Tags = ["Mobile", "Production"]
        }, cancellationToken);

        // 2. Rotate client secret
        var rotateResult = await appService.UpdateClientSecretAsync(
            createResult.Value!.Id, 
            newClientSecret: "NewStrongSecretKey987654!", 
            cancellationToken);

        // 3. Bulk delete applications by identifiers
        var bulkDeleteResult = await appService.BulkDeleteAsync(["app-id-1", "app-id-2"], cancellationToken);

        // 4. Update status for all applications in an environment
        var updateStatusResult = await appService.SetStatusByEnvironmentAsync(
            ApplicationEnvironment.Staging, 
            ApplicationStatus.Suspended, 
            cancellationToken);
    }
}
```

### 2. Configuration Export and Import

```csharp
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;

public class ConfigurationBackup(IConfigurationExportImportService exportImportService)
{
    public async Task BackupAndRestoreAsync(CancellationToken cancellationToken)
    {
        // 1. Export entire configuration (Apps, Scopes, Server Options, Management Options)
        // Note: Package version dynamically resolves to project VersionPrefix (e.g., "1.1.0")
        var exportResult = await exportImportService.ExportConfigurationAsync(cancellationToken);
        ManagementExportPackage package = exportResult.Value!;

        // 2. Or export directly as JSON
        var jsonResult = await exportImportService.ExportConfigurationAsJsonAsync(cancellationToken);
        string json = jsonResult.Value!;

        // 3. Import package with granular options
        var importResult = await exportImportService.ImportConfigurationAsync(package, new ImportOptions
        {
            OverwriteExisting = true,
            ImportApplications = true,
            ImportScopes = true,
            ImportConfigurations = true // Applies OpenIddictServerOptions & Management settings
        }, cancellationToken);

        Console.WriteLine($"Imported {importResult.Value!.ApplicationsCreated} apps and {importResult.Value.ScopesCreated} scopes.");
    }
}
```

### 3. Audit Trail & Logging

```csharp
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;

public class AuditInspection(IAuditTrailStore auditStore)
{
    public async Task ViewAuditEntriesAsync(CancellationToken cancellationToken)
    {
        var result = await auditStore.ListAsync(new AuditFilterRequest
        {
            PageIndex = 1,
            PageSize = 50,
            Category = "Application",
            Action = "Created"
        }, cancellationToken);

        foreach (ManagementAuditEntry entry in result.Value!.Items)
        {
            Console.WriteLine($"[{entry.Timestamp:u}] {entry.Actor} performed {entry.Action} on {entry.Category} '{entry.EntityName}' (Success: {entry.Success})");
        }
    }
}
```

### 4. Implement `IUserAuthenticationProvider`

```csharp
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;

public sealed class AppUserAuthenticationProvider : IUserAuthenticationProvider
{
    public async Task<LoginResult> AuthenticateAsync(
        LoginContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Username == "admin" && context.Password == "P@ssw0rd123!")
        {
            return LoginResult.Success(
                userId: "user-1001",
                username: "admin",
                roles: ["Administrator", "Manager"],
                scopes: ["openid", "profile", "email", "api_access"],
                email: "admin@enterprise.local",
                destinationMode: ClaimDestinationMode.AccessTokenAndIdentityToken
            );
        }

        return LoginResult.Failed("invalid_grant", "The username or password is incorrect.");
    }
}
```

### 5. Programmatically Trigger Token Cleanup

```csharp
app.MapPost("/api/cleanup/trigger", async (ITokenCleanupJobManager cleanupManager) =>
{
    var cleanupResult = await cleanupManager.RunCleanupNowAsync();
    return cleanupResult.IsSuccess
        ? Results.Ok(new { PrunedTokens = cleanupResult.Value })
        : Results.BadRequest(new { Error = cleanupResult.Error.Description });
});
```

### 6. Custom Application Status, Environment & Property Validation

Use `AddApplicationValidation` to enforce operational status and environment constraints across all OpenIddict server login flows (`authorization_code`, `client_credentials`, `password`, `refresh_token`, `device_code`) as well as `OpenIddict.Validation` token checks:

```csharp
builder.Services.AddOpenIddictManagement<MyDbContext>()
    .AddApplicationValidation(options =>
    {
        // 1. Only Active applications can authenticate
        options.RequireStatus(ApplicationStatus.Active);

        // 2. Allow only applications configured for Production or Staging
        options.RequireEnvironments(ApplicationEnvironment.Production, ApplicationEnvironment.Staging);

        // 3. Custom developer validation rules (inspecting tags, roles, extra data, grant types)
        options.ValidateCustom((application, context) =>
        {
            if (application.HasTag("InternalOnly") && context.EndpointType == "Authorization")
            {
                return ApplicationValidationResult.Failed(
                    error: OpenIddictConstants.Errors.UnauthorizedClient,
                    errorDescription: "Internal applications cannot use the interactive authorization endpoint.");
            }

            return ApplicationValidationResult.Success();
        });
    });
```

---

## Related Packages

| Package | Purpose |
|---|---|
| [`Mingoll.OpenIddict.Management.Storage.EfCore`](../OpenIddict.Management.Storage.EfCore/README.md) | Entity Framework Core persistence layer, store implementations, and database entities. |
| [`Mingoll.OpenIddict.Management.Endpoints`](../OpenIddict.Management.Endpoints/README.md) | Pre-configured Minimal API route handlers for application, scope, audit, and configuration operations. |
| [`Mingoll.OpenIddict.Management.Dashboard`](../OpenIddict.Management.Dashboard/README.md) | Embedded Razor Class Library admin UI for visual identity administration and configuration import/export. |
