# OpenIddict.Management.Endpoints

[![NuGet](https://img.shields.io/nuget/v/Mingoll.OpenIddict.Management.Endpoints.svg?style=flat-square&label=NuGet)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Endpoints)
[![Downloads](https://img.shields.io/nuget/dt/Mingoll.OpenIddict.Management.Endpoints.svg?style=flat-square)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Endpoints)
[![Target Frameworks](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

`OpenIddict.Management.Endpoints` (`Mingoll.OpenIddict.Management.Endpoints`) delivers pre-packaged ASP.NET Core Minimal API endpoint route groups for managing OpenIddict client applications, scopes, tokens, authorizations, active user sessions, audit logs, full configuration export/import, and background token cleanup jobs.

Install this package when you need a standardized, secured RESTful management API to power a frontend Single Page Application (React, Angular, Vue, Blazor), an external administrative portal, CLI management tools, or automated infrastructure provisioning scripts.

---

## Installation

Install via the .NET CLI:

```bash
dotnet add package Mingoll.OpenIddict.Management.Endpoints
```

Or via the Visual Studio Package Manager Console:

```powershell
Install-Package Mingoll.OpenIddict.Management.Endpoints
```

---

## Prerequisites & Dependencies

### Supported Frameworks
- **.NET 8.0** (LTS)
- **.NET 9.0** (STS)
- **.NET 10.0**

### Required Package Dependencies
- `Microsoft.AspNetCore.App` (Shared Framework)
- [`Mingoll.OpenIddict.Management.Core`](../OpenIddict.Management.Core/README.md) (Abstractions, models, contracts, validation)

### Companion Persistence Packages
This package requires an implementation of the management service contracts registered in your Dependency Injection container. Most applications pair this with [`Mingoll.OpenIddict.Management.Storage.EfCore`](../OpenIddict.Management.Storage.EfCore/README.md).

---

## Configuration & Setup

Map all management endpoint groups using `MapOpenIddictManagementEndpoints()` on `WebApplication` or any `IEndpointRouteBuilder`.

### Service Registration & Endpoint Mapping (`Program.cs`)

```csharp
using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Endpoints;
using OpenIddict.Management.Endpoints.Extensions;
using OpenIddict.Management.Storage.EfCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Authentication & Authorization policies
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options => { /* Configure token validation */ });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IdentityAdminPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Administrator");
    });
});

// 2. Register storage and Core services with options
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddOpenIddictManagement<ApplicationDbContext>(options =>
{
    options.EnableAuditLogging = true;
});

// 3. Register Health Check (optional)
builder.Services.AddHealthChecks()
    .AddOpenIddictCleanupHealthCheck();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// 4. Map all OpenIddict Management Minimal API endpoints
app.MapOpenIddictManagementEndpoints(options =>
{
    options.RoutePrefix = "/api/management";
    options.AuthorizationPolicy = "IdentityAdminPolicy";
    options.RequireAuthorization = true;
    options.Tags = ["OpenIddict Management API"];
});

app.MapHealthChecks("/health");

app.Run();
```

### Configuration via `appsettings.json`

Settings can be stored in configuration files and bound to `ManagementEndpointOptions`:

```json
{
  "ManagementEndpoints": {
    "RoutePrefix": "/api/management",
    "AuthorizationPolicy": "IdentityAdminPolicy",
    "RequireAuthorization": true,
    "Tags": ["OpenIddict Management API"]
  }
}
```

Bind in `Program.cs`:

```csharp
builder.Services.Configure<ManagementEndpointOptions>(
    builder.Configuration.GetSection("ManagementEndpoints"));

// Uses bound options automatically when configure delegate is omitted or null
app.MapOpenIddictManagementEndpoints();
```

---

## Mapped Endpoint Catalog

When mapped, the package exposes the following REST route groups under the configured `RoutePrefix`:

### Applications (`/applications`)
| Method | Route | Description |
|---|---|---|
| `GET` | `/api/management/applications` | Paginated search of applications (supports `search`, `status`, `environment`, `tags`, sorting). |
| `GET` | `/api/management/applications/{id}` | Get application details by unique identifier. |
| `POST` | `/api/management/applications` | Create a new client application with schema and permissions validation. |
| `PUT` | `/api/management/applications/{id}` | Update an existing application's settings and permissions. |
| `DELETE` | `/api/management/applications/{id}` | Delete an application (soft delete or `?hard=true`). |
| `PATCH` | `/api/management/applications/{id}/status?status=Active` | Update application status (`Active`, `Suspended`, `Revoked`). |
| `POST` | `/api/management/applications/{id}/secret` | Update or rotate application client secret. |
| `POST` | `/api/management/applications/bulk-status` | Bulk update status for all applications matching an environment. |
| `POST` | `/api/management/applications/bulk-create` | Batch create multiple applications in a single request. |
| `POST` | `/api/management/applications/bulk-delete` | Batch delete multiple applications by identifiers. |

### Scopes (`/scopes`)
| Method | Route | Description |
|---|---|---|
| `GET` | `/api/management/scopes` | Paginated list of registered OAuth/OIDC scopes. |
| `GET` | `/api/management/scopes/{id}` | Get scope details by ID. |
| `GET` | `/api/management/scopes/by-name/{name}` | Get scope details by unique scope name. |
| `POST` | `/api/management/scopes` | Register a new scope with associated resources. |
| `PUT` | `/api/management/scopes/{id}` | Update scope metadata and resource server associations. |
| `DELETE` | `/api/management/scopes/{id}` | Delete a scope. |
| `POST` | `/api/management/scopes/bulk-create` | Batch create multiple scopes. |
| `POST` | `/api/management/scopes/bulk-delete` | Batch delete multiple scopes by identifiers. |

### Configuration Export & Import (`/configuration`)
| Method | Route | Description |
|---|---|---|
| `GET` | `/api/management/configuration/export` | Exports a structured JSON backup package (`ManagementExportPackage` with dynamic `VersionPrefix`, e.g. `1.1.0`) containing applications, scopes, OpenIddict Server options, and Management options. |
| `POST` | `/api/management/configuration/import` | Imports and applies an exported configuration package. Supports query flags: `?overwrite=false`, `?importApplications=true`, `?importScopes=true`, `?importConfigurations=true`. |

### Audit Trail (`/audit`)
| Method | Route | Description |
|---|---|---|
| `GET` | `/api/management/audit` | Paginated search of historical security and administrative events. Filter by `search`, `category`, `action`, `entityId`, `actor`, `success`, `fromDate`, `toDate`. *(Blocked with 403 Forbidden when `EnableAuditLogging = false`)*. |

### Tokens & Analytics (`/tokens`)
| Method | Route | Description |
|---|---|---|
| `GET` | `/api/management/tokens` | Paginated token query (filter by `search`, `status`, `tokenType`, `clientId`, `userId`). |
| `GET` | `/api/management/tokens/counts` | Summary counts of valid, expired, and revoked tokens. |
| `GET` | `/api/management/tokens/by-application` | Token distribution grouped by client application. |
| `GET` | `/api/management/tokens/timeline` | Daily token issuance activity over a given date range. |
| `POST` | `/api/management/tokens/revoke-filtered` | Bulk revoke tokens matching filter criteria. |
| `DELETE` | `/api/management/tokens/{id}` | Revoke a single token by ID. |

### Revocation & Sessions (`/revocation`, `/sessions`)
| Method | Route | Description |
|---|---|---|
| `POST` | `/api/management/revocation/by-user` | Revoke all tokens and authorizations for a specific user ID. |
| `POST` | `/api/management/revocation/by-client` | Revoke all tokens issued to a client ID. |
| `POST` | `/api/management/revocation/by-session` | Revoke authorizations matching a session ID. |
| `POST` | `/api/management/revocation/prune` | Manually prune expired/revoked tokens from the database. |
| `GET` | `/api/management/sessions` | List active sessions with filters. |

### Overview & Token Cleanup (`/overview`, `/cleanup`)
| Method | Route | Description |
|---|---|---|
| `GET` | `/api/management/overview` | High-level metrics (total apps, active tokens, scopes, health). |
| `GET` | `/api/management/cleanup/status` | Current status, schedule, batch size, and cumulative pruning telemetry. |
| `POST` | `/api/management/cleanup/toggle?enabled={bool}` | Pause or enable the background cleanup worker. |
| `PUT` | `/api/management/cleanup/settings` | Update cleanup batch size, interval schedule, and includeRevoked flag. |
| `POST` | `/api/management/cleanup/run` | Manually trigger an immediate background cleanup cycle. |

---

## Usage Examples

### 1. Export Configuration via HTTP

```http
GET /api/management/configuration/export HTTP/1.1
Host: localhost:5001
Authorization: Bearer <ADMIN_JWT_TOKEN>
Accept: application/json
```

Response:

```json
{
  "version": "1.1.0",
  "exportedAt": "2026-09-18T00:00:00Z",
  "applications": [ ... ],
  "scopes": [ ... ],
  "openIddictServer": {
    "issuer": "https://auth.example.com/",
    "requireProofKeyForCodeExchange": true,
    "accessTokenLifetime": "01:00:00"
  },
  "management": {
    "routePrefix": "/api/management",
    "requireHttps": true,
    "enableAuditLogging": true
  }
}
```

### 2. Bulk Delete Applications via HTTP

```http
POST /api/management/applications/bulk-delete HTTP/1.1
Host: localhost:5001
Authorization: Bearer <ADMIN_JWT_TOKEN>
Content-Type: application/json

[
  "c6a8f192-3d84-46c5-8495-2a81831c4f52",
  "b8e76a14-8742-4911-9a13-8a35c911a3b1"
]
```

Response:

```json
{
  "totalProcessed": 2,
  "successfulCount": 2,
  "failedCount": 0,
  "errors": []
}
```

---

## Related Packages

| Package | Purpose |
|---|---|
| [`Mingoll.OpenIddict.Management.Core`](../OpenIddict.Management.Core/README.md) | Shared contracts, DTOs, domain models, and validation filters. |
| [`Mingoll.OpenIddict.Management.Storage.EfCore`](../OpenIddict.Management.Storage.EfCore/README.md) | Entity Framework Core persistence implementations backing the API. |
| [`Mingoll.OpenIddict.Management.Dashboard`](../OpenIddict.Management.Dashboard/README.md) | Embedded Razor Class Library admin dashboard UI consuming these services. |
